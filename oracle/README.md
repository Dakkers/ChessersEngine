# ChessersEngine Oracle (Phase 0)

Turns the current C# engine into a **reproducible behavioral reference** for the Rust rewrite.
It drives every `TestScenarios` fixture through the engine and writes canonical, id-sorted JSON
that the Rust port must reproduce byte-for-byte. No engine changes are required for the rules
and codec corpora.

## Run

Requires the .NET 10 SDK (matches the engine's target framework and CI). From the repo root:

```bash
dotnet run --project oracle -- ./oracle/golden
```

The first run restores NUnit transitively (via the engine project reference), then emits:

```
oracle/golden/
  manifest.json                     # the 45 discovered fixtures + settings
  codec/tile-codec.json             # tile id -> (row,col) -> id round-trip, ids -36..63
  rules/<Fixture>.<Setting>.json    # 45 fixtures x 4 deathjump settings = 180 files
  ai/<Fixture>.L<level>.json        # ONLY after the Determinism patch below
```

Console output ends with an **Expectations** section that replays the intended outcomes mined
from the fixture prose (`Checkmate1`, `Stalemate1`) and prints PASS/FAIL.

## What each rules file contains

- `pieces` — canonical board snapshot (sorted by id; volatile `matchId`/`guid` omitted).
- `moveGen` — pseudo-legal target tiles for every active piece (both colors), sorted.
- `moves` — every legal single move for the side to move, applied on a **fresh match**, with the
  full `MoveResult` (incl. `CreateNotation()`) and the resulting board.
- `terminal` + `boardValue` — game-over/winner/draw flags and the deterministic eval.

Each fixture is captured under **all four** `DeathjumpSetting` values because the fixtures don't
encode the setting (they'd otherwise silently run `OFF`) and deathjump legality is config-dependent.

## Why the rules corpus is deterministic with no engine changes

`GetPotentialTilesForMovement`, `MoveChessman`, `CreateNotation`, and the terminal checks never
touch `System.Random`. Their only nondeterminism is `Dictionary` iteration order, which the oracle
removes by sorting every emitted collection by id. That's the whole safety net for move-gen, move
execution, notation, and terminal detection.

> The codec table intentionally reports `roundTripFailures`. The negative-id branches in
> `Helpers.GetColumn/GetRow` have silent `else` cases (return `-1`/`8`), so some ids may not
> round-trip — the table surfaces exactly which, which is the first spec that arithmetic has ever had.

## Enabling the AI corpus (opt-in engine patch)

The minimax shuffles candidates and iterates `Dictionary.Values`, so its chosen move varies run to
run. The oracle auto-detects a `ChessersEngine.Determinism` type; add it plus three guarded edits
(normal gameplay is untouched when the flag is off) and the `ai/` corpus starts emitting.

**1. New file `Determinism.cs`:**
```csharp
namespace ChessersEngine { public static class Determinism { public static bool Enabled = false; } }
```

**2. `Helpers.Shuffle` (Helpers.cs:352) — stable no-op under the flag:**
```csharp
public static void Shuffle<T> (Random rng, List<T> array) {
    if (Determinism.Enabled) return;   // caller sorts instead
    // ... existing Fisher-Yates ...
}
```

**3. `Match.MinimaxHelper` (Match.cs:399 and :403) — sort instead of shuffle:**
```csharp
if (Determinism.Enabled) availableChessmen.Sort((a, b) => a.id.CompareTo(b.id));
else Helpers.Shuffle(rng, availableChessmen);
// ...
if (Determinism.Enabled) potentialTiles.Sort((a, b) => a.id.CompareTo(b.id));
else Helpers.Shuffle(rng, potentialTiles);
```

Two more determinism fixes belong in the same patch (both flagged in the readiness report): a
total-order tiebreak on the checkmate sort at `Move.cs:493`, and dropping the wall-clock
`Board.id` at `Board.cs:21`. The oracle already excludes `matchId` from snapshots, so the latter
only matters if you serialize `CreateMatchData()`.

## How the Rust port uses this

The Rust differential test reads the same JSON, runs the Rust engine on the same fixture, and
asserts equality — move-gen sets, `MoveResult` fields, notation strings, board snapshots, and the
mined expectations. Keep this C# oracle runnable until the Rust port reaches parity; it's also how
you'll regenerate goldens when you deliberately change a rule.
