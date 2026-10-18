# Golden oracle corpus

Frozen behavioral baseline of the C# engine. Each `scenario-<Fixture>.json` is one
seeded AI-vs-AI game recorded from a `TestScenarios` fixture, conforming to
`../schemas/oracle.v1.schema.json`.

This corpus MUST stay byte-identical while the engine is refactored to the id/index
arena representation (dict + back-pointer graph → `Chessman[32]` + occupancy array).
It is the gate that proves each structural step changed shape without changing behavior.

## Regenerate and verify

```
./verify.sh            # regenerate and diff against this baseline; exit 1 on drift
./verify.sh --update   # overwrite the baseline (intentional behavior change only)
```

`verify.sh` requires the .NET SDK on `PATH` (this repo installs it to `~/.dotnet`).

## Canonical parameters

`--all-scenarios --ai-vs-ai --seed 1 --level 2 --quiet`

The output is byte-reproducible from the seed alone: `board.id` is not serialized and
chessmen are iterated in ascending id order, so the RNG is the only source of
nondeterminism reaching the corpus, and `--seed` pins it. Changing the seed, AI level,
or fixture set invalidates the baseline.

## Coverage

42 of 45 fixtures. Three are omitted because they are deliberately partial positions
(e.g. no opposing king) that the engine's game-over and king lookups do not tolerate,
so they cannot be played to completion:

- `InCheckFromCaptureReverseJump`
- `InCheckFromJump`
- `InCheckFromReverseJump`

## Re-baselining

The arena refactor was staged so that every step kept this corpus identical, except one
intentional behavior change: piece iteration switched from the schema insertion order to
canonical id-ascending order (what the Rust port uses). That step re-baselined via
`./verify.sh --update`; its diff is the record of the behavior change.

Re-baseline again only for an intentional behavior change, and review the diff.
