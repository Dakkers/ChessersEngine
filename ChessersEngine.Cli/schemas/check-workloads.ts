// Validates the benchmark workload manifest (ChessersEngine.Bench/workloads.v1.json), the
// hand-authored source file both the C# harness and a future Rust port read. The Zod schema below
// mirrors the published workloads.v1.schema.json; keep the two in lockstep.
//
// Only the manifest's data shape is checked here. The engine-coupled manifest checks -- that every
// fixture resolves to a TestScenarios method, that search fixtures are playable positions, that
// every id dispatches to a WorkloadRegistry kind, and that the replay fixture still replays --
// stay in ChessersEngine.Tests, since they need the engine.

import { readFileSync } from "node:fs";
import { Command } from "commander";
import { z } from "zod";

const layer = z.enum(["micro", "search", "e2e"]);

const workloadSpec = z.strictObject({
  id: z.string().min(1),
  layer,
  description: z.string(),
  fixture: z.string().nullable(), // a TestScenarios method name, or null for the standard opening
  level: z.int().min(0).optional(), // present only where it applies (search/e2e)
  warmup: z.int().min(0),
  measured: z.int().min(1),
  samples: z.int().min(1),
  countersComparable: z.boolean().optional(), // absent means true
});

const manifestV1 = z.strictObject({
  version: z.literal(1),
  workloads: z.array(workloadSpec).min(1),
});

type ManifestV1 = z.infer<typeof manifestV1>;

// Workloads whose counters depend on the engine's seeded System.Random stream: only MinimaxHelper
// advances the RNG, so any workload that searches is affected. These opt out of counter comparison
// (countersComparable:false); every other workload must stay comparable. Kept in lockstep with the
// manifest -- this is the assertion the C# OnlyRngDependentWorkloadsOptOutOfCounterComparison test
// used to make.
const RNG_DEPENDENT_IDS = [
  "search.level0.opening",
  "search.level1.Multijump1",
  "search.level2.opening",
  "search.level2.AlmostCheckmate1",
  "search.level2.Multijump1",
  "selfplay.level0",
];

function duplicateIds(manifest: ManifestV1): string[] {
  const seen = new Set<string>();
  const dupes = new Set<string>();
  for (const w of manifest.workloads) {
    if (seen.has(w.id)) dupes.add(w.id);
    seen.add(w.id);
  }
  return [...dupes].toSorted();
}

function counterComparabilityErrors(manifest: ManifestV1): string[] {
  const optedOut = new Set(
    manifest.workloads.filter((w) => w.countersComparable === false).map((w) => w.id),
  );
  const expected = new Set(RNG_DEPENDENT_IDS);

  const errs: string[] = [];
  for (const id of [...optedOut].toSorted()) {
    if (!expected.has(id)) {
      errs.push(`${id}: marked countersComparable:false but is not a known RNG-dependent workload`);
    }
  }
  for (const id of [...expected].toSorted()) {
    if (!optedOut.has(id)) {
      errs.push(`${id}: is RNG-dependent but is not marked countersComparable:false`);
    }
  }
  return errs;
}

function check(filePath: string): number {
  let doc: unknown;
  try {
    doc = JSON.parse(readFileSync(filePath, "utf8"));
  } catch (e) {
    process.stderr.write(`${filePath}: could not read/parse JSON: ${(e as Error).message}\n`);
    return 2;
  }

  const parsed = manifestV1.safeParse(doc);
  if (!parsed.success) {
    process.stdout.write("Workloads manifest check FAILED:\n");
    for (const issue of parsed.error.issues) {
      process.stdout.write(`  - /${issue.path.join("/")}: ${issue.message}\n`);
    }
    return 1;
  }

  const errs = [
    ...duplicateIds(parsed.data).map((id) => `duplicate workload id: ${id}`),
    ...counterComparabilityErrors(parsed.data),
  ];

  if (errs.length) {
    process.stdout.write("Workloads manifest check FAILED:\n");
    for (const e of errs) process.stdout.write(`  - ${e}\n`);
    return 1;
  }

  process.stdout.write(
    `Workloads manifest check OK: ${parsed.data.workloads.length} workload(s) valid.\n`,
  );
  return 0;
}

new Command()
  .name("check-workloads")
  .description("Validate the benchmark workload manifest against workloads.v1.schema.json.")
  .argument("<file>", "path to workloads.v1.json")
  .action((file: string) => process.exit(check(file)))
  .parse();
