# Performance Oracle — Design

Date: 2026-08-29
Status: implemented; this document describes what was built

## Purpose

The repo already has a **correctness** oracle (`ChessersEngine.Cli`): a golden
corpus of recorded games the Rust port replays to prove it behaves identically.
This document specifies the matching **performance** oracle: a benchmark harness
that measures the C# engine and emits a versioned JSON baseline, so the Rust
port can be measured the same way and the two compared.

The deliverable is not a number. It is a *portable methodology* — a manifest of
workloads, a counting contract, and an output schema — that both languages
implement, plus a comparator that joins two results.

## Goals

- Answer "is the Rust port faster, and by how much?" per workload.
- Answer "is the Rust port doing the *same work*?" independent of hardware, via
  deterministic counters.
- Localise a disappointing speedup to a layer (primitive / search / end-to-end).

## Non-goals

- No CI performance regression gate. Shared runners are too noisy for wall-clock
  gating, and gating on counters alone is a correctness check wearing a perf hat.
- No cross-machine time comparison. The comparator warns rather than normalises.
- No profiling, flamegraphs, or allocation tracking beyond the counters below.

## 1. Project layout

New project `ChessersEngine.Bench/` (`AssemblyName` `chessers-bench`, `net10.0`,
`OutputType` `Exe`), added to `ChessersEngine.sln`.

It MUST NOT use a `ProjectReference` to `ChessersEngine.csproj`. It compiles the
root engine sources directly:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);BENCH</DefineConstants>
</PropertyGroup>
<ItemGroup>
  <Compile Include="..\*.cs" />
</ItemGroup>
```

`DefineConstants` is per-project and does not flow across a `ProjectReference`,
so a referenced engine would link *without* `BENCH` and silently report all-zero
counters. Source-inclusion makes the instrumented build self-contained and
impossible to misconfigure. The cost is that the engine compiles twice in a full
solution build.

`ChessersEngine.csproj` keeps its `DefaultItemExcludes` pattern; the new
`ChessersEngine.Bench/**` directory MUST be added to that exclusion list so the
engine project does not glob the bench sources.

## 2. Counters

New root file `EngineCounters.cs`, part of the engine assembly. Every increment
entry point is a `void` method marked `[Conditional("BENCH")]`, so the C#
compiler erases the call — argument evaluation included — in any build without
the symbol. `ChessersEngine.Cli` and `ChessersEngine.Tests` therefore pay
nothing.

```csharp
[Conditional("BENCH")] public static void Node ();
[Conditional("BENCH")] public static void MoveGen (int tilesProduced);
[Conditional("BENCH")] public static void BoardClone ();
[Conditional("BENCH")] public static void CopyState ();
[Conditional("BENCH")] public static void MoveApply ();
[Conditional("BENCH")] public static void MoveUndo ();
[Conditional("BENCH")] public static void Eval ();

public static void Reset ();           // NOT Conditional — the harness calls it
public static CounterSnapshot Take (); // returns all-zero when BENCH is off
```

Counters are plain `static long` fields with non-atomic increments. The harness
is single-threaded; atomics would add cost for no benefit.

### Increment contract

This table is the cross-language contract. The Rust port MUST increment at
semantically equivalent points, behind a `bench-counters` cargo feature.

| Counter         | Incremented at                                      | Once per |
| --------------- | --------------------------------------------------- | -------- |
| `nodes`         | entry to `Match.MinimaxHelper`                       | call     |
| `moveGen`       | return of `Board.GetPotentialTilesForMovement`       | call     |
| `tilesProduced` | same site, by the returned list's length             | tile     |
| `boardClones`   | `Board.CreateCopy`                                   | call     |
| `copyStates`    | `Board.CopyState`                                    | call     |
| `moveApplies`   | `Move.GetPseudoLegalMoveResult`                      | call     |
| `moveUndos`     | `Board.UndoMove`                                     | call     |
| `evals`         | `Board.CalculateBoardValue`                          | call     |

`CreateCopy` internally calls `CopyState`, so `copyStates >= boardClones` by
construction. That relationship is part of the contract, not an accident.

### Counter overhead

Counters stay live during timed runs — there is one build, not two. Rather than
assume the increments are free, the suite includes a `counter.overhead` workload
that times a bare increment loop. The overhead is then a measured field in the
output instead of an unverified claim, and it is symmetric across both languages.

## 3. Workload manifest

A checked-in `ChessersEngine.Bench/workloads.v1.json`, read by the C# bench and
later by the Rust bench. Iteration counts live in the manifest and are **fixed**,
never adaptively calibrated: adaptive counts would make the two languages
perform different amounts of work, destroying comparability.

Each entry: `id`, `layer` (`micro` | `search` | `e2e`), `description`, `fixture`
(a `TestScenarios` method name, or `null` for the standard opening), `level` (a
flat int: the AI search level, `-1` when not applicable), the optional
`countersComparable` (default `true`, see below), and `warmup` / `measured` /
`samples`.

The manifest is validated on load — iteration counts, layer, id-to-implementation,
and fixture resolution — and a bad manifest exits 2 with every problem listed.

### Counter comparability

`Match.MinimaxHelper` shuffles the piece and tile lists with the match RNG and
prunes on alpha-beta cutoffs, so a search's node count follows the exact
`rng.Next` sequence. A port that does not bit-reproduce .NET's `System.Random`
will legitimately count differently. Workloads with that dependency — the five
`search.*` entries and `selfplay.level0` — declare `"countersComparable": false`.
The flag rides through to `perf.v1.json`; the comparator reports differences on
those workloads in a separate section without failing. Every other workload
defaults to `true` and still fails on any counter difference.

### micro

Per-call primitives, run over a fixed position:

- `movegen.potential` — `Board.GetPotentialTilesForMovement` across every active piece
- `movegen.valid` — `Board.GetValidTilesForMovement` (clone + pseudo-legal + undo per tile)
- `board.clone` — `Board.CreateCopy`
- `board.copystate` — `Board.CopyState`
- `board.apply-undo` — `Move.GetPseudoLegalMoveResult` followed by `Board.UndoMove`
- `eval.boardvalue` — `Board.CalculateBoardValue`
- `notation.create` — `MoveResult.CreateNotation`
- `counter.overhead` — bare increment loop (see above)

`Board.IsGameOver` is deliberately excluded: it is two null/flag checks and would
measure nothing but loop overhead.

### search

`Match.CalculateBestMove` at levels 0, 1, 2 over a handful of `TestScenarios`
fixtures chosen for branching-factor variety. Level 2 is `maxDepth: 4` and gets
few samples. Level 1 differs from level 0 only by `allowMultijumps`, so its entry
runs against the checker-rich `Multijump1` fixture; from the standard opening the
two levels would measure the identical thing.

### e2e

- `replay.game` — replay `ChessersEngine.Bench/replay-sequence.v1.json`, a committed
  move-sequence fixture, turn by turn through `Match.MoveChessman`. The sequence is
  checked in rather than recorded at setup so both implementations replay the same
  game; it was generated from a seeded level-0 self-play run and must round-trip.
- `selfplay.level0` — one seeded AI-vs-AI game at level 0, capped at 40 turns

Fixtures are resolved by reflection over `TestScenarios`, matching the pattern
already in `ChessersEngine.Cli/Program.cs` (`public static MatchData`, zero args).
Each fixture is invoked fresh per iteration so no mutable state leaks between runs.

## 4. Measurement method

Per workload: `warmup` untimed iterations, then `samples` batches of `measured`
timed iterations. Reported per-op figures are batch time divided by `measured`.

- Monotonic clock (`Stopwatch`), single-threaded.
- Tiered compilation is disabled (`<TieredCompilation>false</TieredCompilation>`).
  Its background, elapsed-time-gated promotion made a workload's timing depend on
  where it sat in the run — up to 7x — and no amount of warmup fixes that, because
  the gate is wall-clock rather than iteration count. `environment.jitMode` records
  the mode the numbers were produced under.
- A forced GC between samples, **outside** the timed region. This is a
  language-local step; the Rust implementation has no equivalent and needs none.
  It is documented as such so the methodologies are not mistaken for divergent.
- Reported statistics: `min`, `median`, `p90`, `max`, `mean`, `stddev` of
  ns-per-op, plus `opsPerSecond` derived from `min`.
- **`min` is the headline figure.** It is the sample least contaminated by
  scheduler noise, GC, and frequency scaling.
- Counters are captured from a single dedicated pass per workload (reset, run one
  iteration, snapshot), so a counter value is per-op and not per-batch.

## 5. Output

`perf.v1.json`, following the oracle's conventions exactly: the format version
lives in the `$schema` URI, a machine-readable `schemaVersion` int mirrors it, a
JSON Schema (draft 2020-12) lives beside the existing ones, and a TypeScript+Zod
CI check enforces that the two stay in sync and that emitted files validate.

New files under `ChessersEngine.Cli/schemas/`:
- `perf.v1.schema.json`
- `check-perf.ts` (mirrors `check-oracle.ts`)
- `compare-perf.ts` (see section 6)

Shared Zod definitions go in the existing `schemas.ts`; shared check plumbing in
`check-common.ts`. `package.json` gains `check:perf` and `compare:perf` scripts.

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas/perf.v1.schema.json",
  "schemaVersion": 1,
  "engine": "ChessersEngine (C#)",
  "environment": {
    "os": "darwin", "arch": "arm64", "cpuModel": "Apple M2 Pro",
    "logicalCores": 12, "runtime": ".NET 10.0.0", "buildConfig": "Release",
    "gitSha": "bf3b014", "jitMode": "tiered-disabled", "countersEnabled": true
  },
  "manifest": { "version": 1, "sha256": "..." },
  "results": [
    {
      "id": "search.level2.Checkmate1",
      "layer": "search",
      "iterations": { "warmup": 3, "measured": 10, "samples": 7 },
      "countersComparable": false,
      "time": {
        "minNsPerOp": 412300, "medianNsPerOp": 418900, "p90NsPerOp": 431000,
        "maxNsPerOp": 502100, "meanNsPerOp": 421700, "stddevNsPerOp": 24100,
        "opsPerSecond": 2425.4
      },
      "counters": {
        "nodes": 18422, "moveGen": 91230, "tilesProduced": 402911,
        "boardClones": 18422, "copyStates": 18422, "moveApplies": 91230,
        "moveUndos": 91230, "evals": 4711
      }
    }
  ]
}
```

`manifest.sha256` is the hash of `workloads.v1.json`, so the comparator can
refuse to compare two runs configured differently.

`environment` records no hostname or username.

## 6. Comparator

`compare-perf.ts <baseline.json> <candidate.json>` validates both against the
schema, joins results on `id`, and prints a per-workload table. It distinguishes
two signals that must not be conflated:

- **Counter mismatch** — the two engines did different algorithmic work. This is
  a porting bug, not a performance result. Non-zero exit. Workloads flagged
  `countersComparable: false` are exempt: their differences are printed in a
  labelled section and do not affect the exit code.
- **Time delta** — reported as a speedup factor (`baseline.min / candidate.min`).
  Never causes a non-zero exit on its own.

Additional behaviour:
- A loud warning banner when the two `environment` blocks disagree on `os`,
  `arch`, `cpuModel`, or `jitMode` — the times are then not comparable.
- A hard error when `manifest.sha256` differs between the two files.
- Workload ids present in one file and not the other are listed explicitly, never
  silently dropped.

## 7. Testing

NUnit tests in `ChessersEngine.Tests`:
- Statistics helpers (`median`, `p90`, `stddev`) against hand-computed inputs,
  including the single-sample and even-length edge cases.
- `EngineCounters.Reset` / `Take` semantics, and that `Take` returns zeros when
  `BENCH` is undefined (which is the case in the test assembly).
- Manifest integrity: parses, ids are unique, every `id` maps to a
  `WorkloadRegistry` kind, every `fixture` resolves to a real `TestScenarios`
  method, every `layer` is a known value, and validation rejects each of those
  faults when injected.
- That the committed replay fixture still replays cleanly.

`chessers-bench --smoke` runs every workload at one warmup / one measured / one
sample, for a fast "does it all still execute" check.

## 8. Documentation

`ChessersEngine.Bench/README.md` carries the portable methodology spec: how to
run it, the counter increment contract from section 2, the measurement rules from
section 4, exactly what one timed op is for each workload kind, the replay
fixture's format, and what the Rust implementation has to match. It is the document a
future Rust bench is written against.
