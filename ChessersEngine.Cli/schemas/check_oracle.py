#!/usr/bin/env python3
"""Validate oracle files and enforce that their version markers are in sync.

For each oracle JSON file it checks that:
  1. `$schema` ends with `oracle.vN.schema.json`,
  2. the machine-readable `schemaVersion` field equals that N,
  3. the referenced schema file actually exists under the schemas dir, and
  4. the file validates against that schema (which `const`-pins both markers).

Usage:
  check_oracle.py <schemas_dir> <oracle.json> [more.json ...]

Requires `jsonschema` (pip install jsonschema). Run by CI; also usable locally.
"""
import json
import pathlib
import re
import sys

from jsonschema import Draft202012Validator

SCHEMA_URI_RE = re.compile(r"oracle\.v(\d+)\.schema\.json$")


def check(oracle_path, schemas_dir):
    errs = []
    doc = json.loads(pathlib.Path(oracle_path).read_text())

    schema_uri = doc.get("$schema")
    if not isinstance(schema_uri, str):
        return [f"{oracle_path}: missing or non-string $schema"]

    m = SCHEMA_URI_RE.search(schema_uri)
    if not m:
        return [f"{oracle_path}: $schema must end with oracle.vN.schema.json (got {schema_uri!r})"]
    uri_version = int(m.group(1))

    field_version = doc.get("schemaVersion")
    if field_version != uri_version:
        errs.append(
            f"{oracle_path}: schemaVersion={field_version!r} is out of sync with "
            f"$schema (v{uri_version}); the two must match"
        )

    schema_file = schemas_dir / f"oracle.v{uri_version}.schema.json"
    if not schema_file.exists():
        errs.append(f"{oracle_path}: $schema references v{uri_version} but {schema_file} is missing")
        return errs

    schema = json.loads(schema_file.read_text())
    try:
        Draft202012Validator.check_schema(schema)
    except Exception as exc:  # noqa: BLE001 - report any malformed schema
        return errs + [f"{schema_file}: schema is itself invalid: {exc}"]

    validator = Draft202012Validator(schema)
    for e in sorted(validator.iter_errors(doc), key=lambda e: list(e.path)):
        errs.append(f"{oracle_path}: schema violation at /{'/'.join(map(str, e.path))}: {e.message}")

    return errs


def main(argv):
    if len(argv) < 3:
        print(__doc__.strip(), file=sys.stderr)
        return 2

    schemas_dir = pathlib.Path(argv[1])
    files = argv[2:]

    all_errs = []
    for path in files:
        all_errs.extend(check(path, schemas_dir))

    if all_errs:
        print("Oracle schema check FAILED:")
        for e in all_errs:
            print(f"  - {e}")
        return 1

    print(f"Oracle schema check OK: {len(files)} file(s) valid and version-synced.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
