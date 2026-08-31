# chessers-bench — the performance oracle

The sibling of the correctness oracle in [`../ChessersEngine.Cli`](../ChessersEngine.Cli).
That one proves the Rust port *behaves* like the C# engine; this one measures how
fast each of them is, and whether they do the same amount of work getting there.

This document is the portable specification. A Rust benchmark that follows it
produces a `perf.v1.json` the comparator can diff against the C# baseline.

## Running

```bash
export PATH="$HOME/.dotnet:$PATH"

# the full suite (~13 seconds), writing ./perf/perf.v1.json
dotnet run --project ChessersEngine.Bench --configuration Release

# every workload once, no warmup — a wiring check, not a measurement
dotnet run --project ChessersEngine.Bench --configuration Release -- --smoke

# just the search layer
dotnet run --project ChessersEngine.Bench --configuration Release -- --filter search
```

**Always use `--configuration Release`.** A Debug build measures the absence of
the optimiser.

Options: `--out <dir>` (default `perf`), `--manifest <path>`, `--filter <text>`,
`--list`, `--smoke`, `--record-replay <path>`, `--help`.

### Reproducibility: tiered compilation is off

.NET promotes methods to fully optimised code on a background, *elapsed-time*-gated
delay, so a workload measured early in the process never reaches steady state no
matter how many warmup iterations it runs. That made timings depend on run order
by up to 7x. `ChessersEngine.Bench.csproj` therefore sets
`<TieredCompilation>false</TieredCompilation>`, which flattens the order effect to
1.00x; the `environment.jitMode` field records that the numbers were produced that
way (`"tiered-disabled"`).

`jitMode` is part of what the two implementations must agree on. A Rust bench
should record how *it* was compiled — a release profile with a fixed `opt-level`
and no PGO is the analogue — and the comparator warns when the two differ.

Disabling tiered compilation also disables dynamic PGO and OSR, .NET's
profile-guided re-optimisation passes. So the C# numbers here are fully
*optimised* but not profile-guided, and they are slower than production C#
because of it — sometimes by a lot. Real measured examples from this repo:
`selfplay.level0` went from 273 ms to 772 ms, and `board.clone` from 4781 ns to
9950 ns, once tiered compilation was turned off. Consequently, the
C#-baseline-vs-Rust speedup this harness prints is an **upper bound** that
flatters the port: it must not be quoted as "Rust is N times faster than C#"
without that caveat.

## Comparing two runs

```bash
cd ChessersEngine.Cli/schemas
pnpm install --frozen-lockfile
node compare-perf.ts path/to/csharp/perf.v1.json path/to/rust/perf.v1.json
```

The comparator keeps two signals apart:

- **counter mismatch** — the two engines did different algorithmic work. That is a
  porting bug, not a performance result, and it exits non-zero. The exception is a
  workload marked `countersComparable: false` (see below): its counter difference
  is printed in its own section and does **not** fail the run.
- **time delta** — reported as a speedup factor. It never fails on its own, and it
  is prefixed with a warning banner when the two `environment` blocks disagree on
  OS, architecture, CPU model, or `jitMode`.

It refuses outright to compare runs whose `manifest.sha256` differs.

## How counting works

`EngineCounters` ([`../EngineCounters.cs`](../EngineCounters.cs)) is a set of
`static long` tallies. Every increment method is `[Conditional("BENCH")]`, so the
C# compiler erases the call — argument evaluation included — in any build without
that symbol. Only this project defines it, by compiling the engine sources in
directly rather than referencing the engine project (`DefineConstants` does not
flow across a `ProjectReference`, so a reference would link an uninstrumented
engine and report zeros).

`chessers-bench` refuses to emit a report if `EngineCounters.Enabled` is false, or
if a probe run — a level-0 search from the standard opening, which exercises all
eight — leaves any counter at zero. It names the counter that failed. A report
full of plausible zeros is worse than no report.

### The increment contract

A Rust port MUST increment at semantically equivalent points, behind a
`bench-counters` cargo feature.

| Counter         | Incremented at                                  | Once per |
| --------------- | ------------------------------------------------ | -------- |
| `nodes`         | entry to `Match.MinimaxHelper`                    | call     |
| `moveGen`       | every return of `Board.GetPotentialTilesForMovement` | call |
| `tilesProduced` | same site, by the returned list's length          | tile     |
| `boardClones`   | `Board.CreateCopy`                                | call     |
| `copyStates`    | `Board.CopyState`                                 | call     |
| `moveApplies`   | `Move.GetPseudoLegalMoveResult`                   | call     |
| `moveUndos`     | `Board.UndoMove`                                  | call     |
| `evals`         | `Board.CalculateBoardValue`                       | call     |

`CreateCopy` calls `CopyState`, so `copyStates >= boardClones` always holds. That
relationship is part of the contract, not an accident of the current code.

Counters stay live during the timed runs — there is one build, not two. Rather
than assume that is free, the suite includes a `counter.overhead` workload that
times a bare increment loop, so the cost is a measured number in the output.

### Counters that cannot be compared

Six workloads count work that depends on the host language's seeded RNG stream.
`Match.MinimaxHelper` shuffles both the piece list and the tile list with the
match RNG (`Helpers.Shuffle`) and prunes on alpha-beta cutoffs, so the node count
follows the exact `rng.Next` sequence. A Rust port that does not bit-reproduce
.NET's `System.Random` will search a differently-ordered move list and legitimately
visit a different number of nodes.

Those workloads carry `"countersComparable": false` in the manifest:

| Workload | Why |
| -------- | --- |
| `search.level0.opening` | minimax move ordering is RNG-shuffled |
| `search.level1.Multijump1` | same |
| `search.level2.opening` | same |
| `search.level2.AlmostCheckmate1` | same |
| `search.level2.Multijump1` | same |
| `selfplay.level0` | every turn calls the search above |

The field is optional and **defaults to true**, so every other workload — including
`replay.game`, which replays a committed fixture rather than a fresh self-play game
— still fails the comparison on any counter difference.

A Rust port MUST carry the flag through from the manifest to its own
`perf.v1.json`, so the comparator sees the same classification. It is not licence
to ignore those workloads: their *times* remain fully comparable, and a wild
difference in their counters is still worth reading, just not worth failing on.

## What one timed op is

The manifest declares iteration counts; this section declares the work. A Rust
bench MUST time exactly these bodies, with everything else done in untimed setup.

| Kind | One timed op |
| ---- | ------------ |
| `counter.overhead` | 1000 `EngineCounters.Node()` increments. Its ns/op is therefore **per 1000 increments**, not per increment. |
| `movegen.potential` | `board.GetActiveChessmen()` — **inside** the timed region — then one `GetPotentialTilesForMovement` per returned piece. |
| `movegen.valid` | the same, with `GetValidTilesForMovement`. |
| `board.clone` | one `board.CreateCopy()`. |
| `board.copystate` | one `target.CopyState(board)`, where `target` is a clone built once during setup. |
| `board.apply-undo` | construct a `Move`, call `GetPseudoLegalMoveResult()`, then `board.UndoMove(result)`. |
| `eval.boardvalue` | one `board.CalculateBoardValue(0)`. |
| `notation.create` | one `result.CreateNotation()` on a `MoveResult` produced during setup. |
| `search.*` | construct a fresh `MatchData` from the fixture **and** a fresh `Match` seeded with `1`, then `CalculateBestMove(level)`. The construction is deliberately inside the timed body: `CalculateBestMove` shuffles with the match RNG, so a reused `Match` would search a differently-ordered list on every iteration and the samples would not be measuring the same work. |
| `replay.game` | a fresh `Match` seeded with `1`, then every turn of `replay-sequence.v1.json` fed through `MoveChessman` followed by `CommitTurn`. |
| `selfplay.level0` | one seeded AI-vs-AI game: seed `1`, level `0`, capped at 40 turns (`RecordSelfPlay`). |

The board a micro workload runs against is built once in setup, from the spec's
`fixture` (or the standard opening when `fixture` is `null`).

`board.apply-undo` and `notation.create` both use **the first legal move of the
first white piece**, in the order `Board.GetActiveChessmenOfColor(WHITE)` yields —
which is `chessmenById` dictionary-enumeration order. A Rust port must reproduce
that order, or it will time a different move. The tile is likewise the first
entry of the list `Board.GetValidTilesForMovement(chessman)` returns for that
piece (`WorkloadRegistry.FirstLegalMove` takes `tiles[0]`), so a Rust port must
reproduce that tile ordering too.

### The counter contract is per call, not per overload

`Board.CreateCopy` takes an optional `_pieces` argument and
`Board.GetPotentialTilesForMovement` a `jumpsOnly` flag. Each counter fires
**once per call, regardless of the overload used or the flag's value**. There is
no variant that counts twice, and none that skips counting.

## The replay fixture

[`replay-sequence.v1.json`](replay-sequence.v1.json) is the move sequence
`replay.game` replays. It is a **committed artifact**, not something either
implementation records at setup: a Rust bench recording its own self-play game
would replay a different game, and the two `replay.game` results would then be
timing different work.

```jsonc
{
  "version": 1,
  "description": "...",
  "turns": [
    [{ "pieceId": 2, "tileId": 17, "playerId": 0, "promotionRank": -1 }]
  ]
}
```

A turn is a list of move attempts, because one turn can be several hops of a
checker multi-jump. The four fields are exactly what `MoveAttempt` needs.

It was generated from a level-0 self-play game, seed `1`, capped at 40 turns, and
the generator refuses to write a sequence that does not replay cleanly. To
regenerate it (only necessary if engine rules change and the sequence stops being
legal — which changes `manifest.sha256` consumers, so do it deliberately):

```bash
dotnet run --project ChessersEngine.Bench --configuration Release -- \
  --record-replay ChessersEngine.Bench/replay-sequence.v1.json
```

`ChessersEngine.Tests` replays the committed file on every test run, so a rules
change that invalidates it fails the build rather than the benchmark.

## The workload manifest

[`workloads.v1.json`](workloads.v1.json) declares every workload and its
iteration counts. Both language implementations read this same file, and its
SHA-256 goes into the report so the comparator can refuse mismatched runs.

Iteration counts are **declared, never calibrated at runtime**. An adaptive
"run until N milliseconds elapse" scheme would have the two languages perform
different amounts of work, which is precisely what this oracle exists to rule out.

Fields: `id`, `layer` (`micro` | `search` | `e2e`), `description`, `fixture` (a
`TestScenarios` method name, or `null` for the standard opening), `level` (AI
search level, `-1` when not applicable), `warmup`, `measured`, `samples`, and the
optional `countersComparable` (default `true`).

The manifest is validated on load: `measured >= 1`, `samples >= 1`,
`warmup >= 0`, a known `layer`, an `id` that resolves to a workload
implementation, and a `fixture` that resolves to a `TestScenarios` method. A
manifest that fails any of those is reported line by line and exits 2.

An id is `<kind>.<variant>`; only the kind selects an implementation, so adding a
`movegen.potential.<newFixture>` entry needs no code change.

The three layers exist so that a disappointing speedup can be localised: if the
primitives are 20x faster but end-to-end play is only 3x, the difference is in
how they are composed, not in the primitives.

## Measurement method

For each workload:

1. Build the workload once. Setup — constructing boards, loading the replay
   sequence — happens here and is **not** timed.
2. Run `warmup` untimed iterations.
3. Repeat `samples` times: force a collection (outside the timed region), then
   time a batch of `measured` iterations. The sample's value is batch time
   divided by `measured`.
4. Reset the counters, run one further iteration, and snapshot them. Counters are
   therefore per-op, and their pass is separate from every timed pass.

The forced collection in step 3 is a .NET-local step. A Rust implementation has
no equivalent and needs none; this is a difference in the implementations, not in
the methodology.

Reported per workload: `min`, `median`, `p90`, `max`, `mean`, `stddev` of
nanoseconds per op, plus `opsPerSecond` derived from `min`.

**`min` is the headline figure** — the sample least contaminated by scheduler
noise, garbage collection, and CPU frequency scaling.

`stddev` is the *sample* standard deviation (n−1 denominator), zero for a single
sample. Percentiles use linear interpolation between the two closest ranks, where
`rank = p/100 × (n−1)` over the ascending-sorted samples. Both definitions are
part of the contract; see [`Stats.cs`](Stats.cs).

## Output

`perf.v1.json`, following the same conventions as the game oracle: the format
version lives in the `$schema` URI, a machine-readable `schemaVersion` mirrors it,
and [`check-perf.ts`](../ChessersEngine.Cli/schemas/check-perf.ts) enforces that
the two agree and that the file validates. The published JSON Schema is
[`perf.v1.schema.json`](../ChessersEngine.Cli/schemas/perf.v1.schema.json).

The `environment` block records OS, architecture, CPU model, logical core count,
runtime, build configuration, git SHA, `jitMode`, and whether counters were
compiled in. It deliberately records no hostname or username.

## Not in scope

There is no CI performance gate. Shared runners are too noisy for wall-clock
gating, and gating on counters alone would be a correctness check wearing a
performance hat — the oracle's job is to inform a comparison, not to block a
merge. CI runs `--smoke` only, to prove the harness still executes.
