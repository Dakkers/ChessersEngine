# Oracle schemas

JSON Schemas (draft 2020-12) for the golden-corpus files that `chessers` writes.

The format version lives in the **`$schema` URI** — each oracle file references
the exact schema it was written for, e.g.:

```json
"$schema": ".../ChessersEngine.Cli/schemas/oracle.v1.schema.json"
```

A machine-readable `schemaVersion` field mirrors that version for consumers that
don't parse the URI. [`check-oracle.ts`](check-oracle.ts) (TypeScript + Zod)
enforces that the two markers agree and that files validate; it runs in CI
(`.github/workflows/build.yml`) and can be run locally:

```bash
cd ChessersEngine.Cli/schemas
npm ci
npx tsx check-oracle.ts . path/to/*.json
```

A second artifact family lives here too: `codec.vN.schema.json` for the
`chessers --dump-codec` tile-id coordinate table, validated by
[`check-codec.ts`](check-codec.ts). It follows the same policy below.

A third artifact family lives here: `perf.vN.schema.json` for the `chessers-bench`
performance oracle, validated by [`check-perf.ts`](check-perf.ts) and diffed by
[`compare-perf.ts`](compare-perf.ts). It follows the same policy below. See
[`../../ChessersEngine.Bench/README.md`](../../ChessersEngine.Bench/README.md)
for the measurement methodology the numbers depend on.

A fourth artifact, `workloads.vN.schema.json`, describes the benchmark's
checked-in workload manifest (`ChessersEngine.Bench/workloads.v1.json`),
validated by [`check-workloads.ts`](check-workloads.ts). Unlike the others it is
a hand-authored *source* file rather than an emitted one, so it carries no
`$schema` marker and its Zod mirror lives inline in `check-workloads.ts` rather
than in `schemas.ts`.

The `*.vN.schema.json` files are the language-neutral published contract; the Zod
schemas in [`schemas.ts`](schemas.ts) mirror them and are the validator
implementation — keep the two in lockstep when adding a version.

## Versioning policy

- One file per released version: `oracle.v1.schema.json`, `oracle.v2.schema.json`, …
- **Released versions are immutable.** Once a version has produced corpus files,
  its schema file is never edited again — older files must keep validating.
- A **breaking** change (removing/renaming a field, tightening a type) means a new
  `oracle.vN.schema.json`, a matching Zod schema in `schemas.ts` (registered in the
  `oracleSchemas`/`codecSchemas` map), and bumping the `$schema` the recorder emits
  (`OracleRecorder.cs`). The previous file stays here as the backup for old corpus.
- Purely **additive**, backward-compatible tweaks can be made to the current
  version's file in place (all existing files still validate).

## Current version

`oracle.v1.schema.json` — see [../README.md](../README.md) for the field-by-field
description and an example.

`perf.v1.schema.json` — see [../../ChessersEngine.Bench/README.md](../../ChessersEngine.Bench/README.md).

`workloads.v1.schema.json` — the benchmark workload manifest; see [../../ChessersEngine.Bench/README.md](../../ChessersEngine.Bench/README.md).
