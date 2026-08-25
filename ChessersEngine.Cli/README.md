# chessers — terminal game

A small console front-end that drives `ChessersEngine` so you can **play games
in the terminal**: human vs AI, AI vs AI, or hotseat (two humans).

## Running

This repo has no system-wide dotnet; the SDK lives in `~/.dotnet`:

```bash
export PATH="$HOME/.dotnet:$PATH"
```

Play White against the AI (reproducible via a seed):

```bash
dotnet run --project ChessersEngine.Cli -- --white human --black ai --seed 42
```

Watch a few AI-vs-AI games:

```bash
dotnet run --project ChessersEngine.Cli -- --ai-vs-ai --games 5 --seed 1 --delay 300
```

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
`legend` symbol key · `resign` · `quit`.

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
| `--quiet`           |          | don't render boards (bulk AI-vs-AI runs)            |
| `--no-color`        |          | disable ANSI color (auto-off when piped)            |

## Determinism

With `--seed`, a full game is reproducible across runs and machines (same seed →
identical moves), which makes AI-vs-AI games repeatable.
