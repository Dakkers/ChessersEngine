# Oracle schemas

JSON Schemas (draft 2020-12) for the golden-corpus files that `chessers` writes.

The format version lives in the **`$schema` URI** — each oracle file references
the exact schema it was written for, e.g.:

```json
"$schema": ".../ChessersEngine.Cli/schemas/oracle.v1.schema.json"
```

A machine-readable `schemaVersion` field mirrors that version for consumers that
don't parse the URI. [`check_oracle.py`](check_oracle.py) enforces that the two
markers agree and that files validate; it runs in CI (`.github/workflows/build.yml`)
and can be run locally:

```bash
python3 ChessersEngine.Cli/schemas/check_oracle.py ChessersEngine.Cli/schemas path/to/*.json
```

## Versioning policy

- One file per released version: `oracle.v1.schema.json`, `oracle.v2.schema.json`, …
- **Released versions are immutable.** Once a version has produced corpus files,
  its schema file is never edited again — older files must keep validating.
- A **breaking** change (removing/renaming a field, tightening a type) means a new
  `oracle.vN.schema.json` and bumping the `$schema` the recorder emits
  (`OracleRecorder.cs`). The previous file stays here as the backup for old corpus.
- Purely **additive**, backward-compatible tweaks can be made to the current
  version's file in place (all existing files still validate).

## Current version

`oracle.v1.schema.json` — see [../README.md](../README.md) for the field-by-field
description and an example.
