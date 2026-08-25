# chessers — terminal game + oracle recorder

A small console front-end that drives `ChessersEngine` so you can **play games
in the terminal** (human vs AI, AI vs AI, or hotseat) and **record every game as
a golden-corpus JSON file** for differential testing of the Rust port.

Each turn's move attempt (the input), the engine's full `MoveResult` (every
flag), and a board snapshot after the turn are captured. A port can rebuild the
initial board, replay each recorded attempt, and assert the results and board
states match — no engine determinism required to *consume* the corpus.

## Running

This repo has no system-wide dotnet; the SDK lives in `~/.dotnet`:

```bash
export PATH="$HOME/.dotnet:$PATH"
```

Play White against the AI (reproducible via a seed):

```bash
dotnet run --project ChessersEngine.Cli -- --white human --black ai --seed 42
```

Generate a 50-game AI-vs-AI corpus with no board rendering:

```bash
dotnet run --project ChessersEngine.Cli -- --ai-vs-ai --games 50 --seed 1 --quiet
```

Games are written to `./oracle/` by default (one JSON file per game).

## Move input (at the `move>` prompt)

| Input          | Meaning                                                        |
| -------------- | -------------------------------------------------------------- |
| `e2e4`         | move from e2 to e4 (also `e2 e4` or `e2-e4`)                   |
| `e7e8q`        | promote to queen (`q`/`r`/`b`/`n`; auto-queens if omitted)     |
| `#-14`         | raw tile id — the only way to target off-board *deathjump* tiles |

Deathjump tiles have negative ids and are shown as `#<id>` by `moves <sq>`, so
whatever `moves` prints can be typed straight back. (A bare token like `d7` is
always the square d7, never a deathjump.)

Board coordinates: tile id = `row*8 + col`, with row 0 = rank 1 (White's back
rank) and col 0 = file a. Checker multi-jumps are handled automatically — after
a jump that leaves a legal continuation, you're prompted to jump again with the
same piece until the turn ends.

### Commands

`moves <sq>` list legal targets · `hint` suggest a move · `board` redraw ·
`legend` symbol key · `save` write the game so far · `resign` · `quit`.

Piece symbols: `UPPER`=white, `lower`=black; `" X "`=chess piece,
`"(X)"`=checker, `"[X]"`=kinged checker.

## Options

| Option              | Default  | Meaning                                             |
| ------------------- | -------- | --------------------------------------------------- |
| `--white h/ai`      | `human`  | who controls White                                  |
| `--black h/ai`      | `ai`     | who controls Black                                  |
| `--ai-vs-ai`        |          | shorthand for `--white ai --black ai`               |
| `--hotseat`         |          | two humans on one keyboard                           |
| `--level 0..2`      | `2`      | AI search strength (0 fastest/weakest, 2 strongest) |
| `--seed <int>`      | random   | RNG seed; game N of a batch uses `seed + N`         |
| `--deathjump MODE`  | `off`    | `off` \| `sides` \| `back` \| `all`                 |
| `--games <n>`       | `1`      | play n games back-to-back                           |
| `--max-plies <n>`   | `400`    | adjudicate a draw (`reason: move-limit`) after n plies |
| `--delay <ms>`      | `0`      | pause after each AI turn (for watching)             |
| `--out <dir>`       | `oracle` | output directory for the JSON corpus                |
| `--quiet`           |          | don't render boards (bulk corpus runs)              |
| `--no-color`        |          | disable ANSI color (auto-off when piped)            |

## Oracle JSON schema

Each file opens with a `$schema` reference to a versioned JSON Schema (draft
2020-12) under [`schemas/`](schemas/) — the **format version lives in that URI**
(currently `oracle.v1.schema.json`), so editors and validators can check it and
old files always resolve the schema they were written for. See
[`schemas/README.md`](schemas/README.md) for the versioning policy. A
machine-readable `schemaVersion` field mirrors that version; a CI check
([`schemas/check_oracle.py`](schemas/check_oracle.py)) enforces that the two stay
in sync (and that generated files validate).

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas/oracle.v1.schema.json",
  "schemaVersion": 1,
  "engine": "ChessersEngine (C#)",
  "config": {
    "white": "human", "black": "ai", "aiLevel": 2, "randomSeed": 42,
    "deathjumpSetting": "OFF", "whitePlayerId": 0, "blackPlayerId": 1
  },
  "initialPieces": [ /* ChessmanSchema[] — the starting board */ ],
  "plies": [
    {
      "index": 1,
      "turnColor": "WHITE",          // color to move at the start of this turn
      "playerId": 0,
      "actor": "human",              // "human" | "ai"
      "moves": [                     // >1 entry only for multi-jump turns
        {
          "attempt":  { "pieceId": 0, "tileId": 24, "promotionRank": -1, ... },
          "notation": "0_Pa2a4",
          "result":   { /* full MoveResult: turnChanged, wasPieceJumped,
                           polarityChanged, kinged, isWinningMove, ... */ }
        }
      ],
      "committedNotation": "0_Pa2a4",
      "boardAfter": [ /* ChessmanSchema[] — full board snapshot after commit */ ]
    }
  ],
  "outcome": {
    "gameOver": true, "winningPlayerId": 0, "winningColor": "WHITE",
    "isDraw": false, "isResignation": false, "reason": "checkmate"
  }
}
```

Enums (`ColorEnum`, `ChessmanKindEnum`) serialize by name; other ints match the
engine's `Constants` (e.g. `result.type` is `MOVE_TYPE_*`, `kind` is
`CHESSMAN_KIND_*`). `outcome.reason` is one of `checkmate`, `stalemate`,
`resignation`, `move-limit`, `no-legal-moves`, or `quit`.

`plies` holds only moves that were *accepted*. A top-level `rejectedAttempts`
array records illegal moves the engine turned down (empty for AI-only games):
each entry has the `board` it was tried against, the `attempt`, and the engine's
`result` — `null` when `MoveChessman` returned null (e.g. a target holds your own
piece), otherwise a `MoveResult` with `valid: false`. A port can replay each to
assert it rejects the same attempt the same way.

## Determinism

With `--seed`, a full game is reproducible across runs and machines (verified by
diffing two same-seed games). This makes the corpus regenerable and suitable as
the frozen reference the Rust rewrite is tested against.
