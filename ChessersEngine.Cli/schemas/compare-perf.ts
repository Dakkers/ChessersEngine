// Joins two perf reports -- typically the C# baseline and the Rust candidate -- on workload id.
//
// Two signals are kept apart on purpose:
//   - a counter difference means the two implementations did different algorithmic work; that is a
//     porting bug, and it fails the run.
//   - a time difference is just a result, and never fails on its own.
//
// Workloads flagged `countersComparable: false` are the exception: their counters depend on the
// host language's seeded RNG stream, so a difference there is reported separately and does not
// fail the run.

import { readFileSync } from "node:fs";
import { Command } from "commander";
import { type PerfV1, perfV1 } from "./schemas.ts";

const COUNTER_KEYS = [
  "nodes",
  "moveGen",
  "tilesProduced",
  "boardClones",
  "copyStates",
  "moveApplies",
  "moveUndos",
  "evals",
] as const;

function load(path: string): PerfV1 {
  let doc: unknown;
  try {
    doc = JSON.parse(readFileSync(path, "utf8"));
  } catch (e) {
    process.stderr.write(`${path}: could not read/parse JSON: ${(e as Error).message}\n`);
    process.exit(2);
  }

  const parsed = perfV1.safeParse(doc);
  if (!parsed.success) {
    process.stderr.write(`${path}: not a valid perf.v1 report:\n`);
    for (const issue of parsed.error.issues) {
      process.stderr.write(`  - /${issue.path.join("/")}: ${issue.message}\n`);
    }
    process.exit(2);
  }
  return parsed.data;
}

function envMismatch(a: PerfV1["environment"], b: PerfV1["environment"]): string[] {
  const differing: string[] = [];
  if (a.os !== b.os) differing.push(`os: ${a.os} vs ${b.os}`);
  if (a.arch !== b.arch) differing.push(`arch: ${a.arch} vs ${b.arch}`);
  if (a.cpuModel !== b.cpuModel) differing.push(`cpuModel: ${a.cpuModel} vs ${b.cpuModel}`);
  if (a.jitMode !== b.jitMode) differing.push(`jitMode: ${a.jitMode} vs ${b.jitMode}`);
  return differing;
}

function compare(baselinePath: string, candidatePath: string): number {
  const baseline = load(baselinePath);
  const candidate = load(candidatePath);

  if (baseline.manifest.sha256 !== candidate.manifest.sha256) {
    process.stderr.write(
      "manifest mismatch: the two runs used different workload manifests, so their results " +
        `are not comparable (${baseline.manifest.sha256.slice(0, 12)} vs ` +
        `${candidate.manifest.sha256.slice(0, 12)})\n`,
    );
    return 2;
  }

  const differing = envMismatch(baseline.environment, candidate.environment);
  if (differing.length) {
    process.stdout.write(
      "!! DIFFERENT MACHINES -- the times below are NOT comparable !!\n" +
        differing.map((d) => `   ${d}\n`).join("") +
        "   Counter comparisons remain valid.\n\n",
    );
  }

  const byId = new Map(candidate.results.map((r) => [r.id, r]));
  const missing: string[] = [];
  const extra = new Set(byId.keys());
  const counterProblems: string[] = [];
  const rngCounterNotes: string[] = [];
  const comparableFlagMismatches: string[] = [];

  process.stdout.write(
    `${"workload".padEnd(34)}${"baseline ns".padStart(14)}${"candidate ns".padStart(14)}` +
      `${"speedup".padStart(10)}  counters\n`,
  );

  for (const base of baseline.results) {
    const cand = byId.get(base.id);
    if (!cand) {
      missing.push(base.id);
      continue;
    }
    extra.delete(base.id);

    const deltas = COUNTER_KEYS.filter((k) => base.counters[k] !== cand.counters[k]).map(
      (k) => `${k} ${base.counters[k]}->${cand.counters[k]}`,
    );
    // The comparability decision is manifest-derived, so it must come from the baseline alone --
    // a candidate cannot unilaterally exempt itself from the counter check by flipping the flag.
    const comparable = base.countersComparable;
    if (base.countersComparable !== cand.countersComparable) {
      comparableFlagMismatches.push(
        `${base.id}: countersComparable ${base.countersComparable} -> ${cand.countersComparable}`,
      );
    }
    if (deltas.length) {
      (comparable ? counterProblems : rngCounterNotes).push(`${base.id}: ${deltas.join(", ")}`);
    }

    let counterCell = "  match";
    if (deltas.length) counterCell = comparable ? "  DIFFER" : "  differ (rng)";
    else if (!comparable) counterCell = "  match (rng)";

    const speedup = cand.time.minNsPerOp > 0 ? base.time.minNsPerOp / cand.time.minNsPerOp : 0;
    process.stdout.write(
      base.id.padEnd(34) +
        base.time.minNsPerOp.toFixed(1).padStart(14) +
        cand.time.minNsPerOp.toFixed(1).padStart(14) +
        `${speedup.toFixed(2)}x`.padStart(10) +
        counterCell +
        "\n",
    );
  }

  let failed = false;

  if (missing.length) {
    failed = true;
    process.stdout.write(`\nmissing from ${candidatePath}:\n`);
    for (const id of missing) process.stdout.write(`  - ${id}\n`);
  }
  if (extra.size) {
    failed = true;
    process.stdout.write(`\nnot present in ${baselinePath}:\n`);
    for (const id of extra) process.stdout.write(`  - ${id}\n`);
  }
  if (counterProblems.length) {
    failed = true;
    process.stdout.write("\ncounter mismatches (the two engines did different work):\n");
    for (const p of counterProblems) process.stdout.write(`  - ${p}\n`);
  }
  if (comparableFlagMismatches.length) {
    failed = true;
    process.stdout.write(
      "\ncountersComparable disagreement (both reports share a manifest, so this is itself a bug):\n",
    );
    for (const m of comparableFlagMismatches) process.stdout.write(`  - ${m}\n`);
  }

  if (rngCounterNotes.length) {
    process.stdout.write(
      "\ncounter differences on RNG-dependent workloads (reported only, not a failure):\n",
    );
    for (const n of rngCounterNotes) process.stdout.write(`  - ${n}\n`);
    process.stdout.write(
      "  These workloads are flagged countersComparable: false because their node counts follow\n" +
        "  the host language's seeded RNG stream, which no port is expected to bit-reproduce.\n",
    );
  }

  if (!counterProblems.length) {
    process.stdout.write("\nCounters match on every comparable workload.\n");
  }
  return failed ? 1 : 0;
}

new Command()
  .name("compare-perf")
  .description("Compare two perf.v1 reports; fails on counter mismatch, never on time alone.")
  .argument("<baseline>", "baseline perf.v1.json (e.g. the C# engine)")
  .argument("<candidate>", "candidate perf.v1.json (e.g. the Rust port)")
  .action((baseline: string, candidate: string) => process.exit(compare(baseline, candidate)))
  .parse();
