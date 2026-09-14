#!/usr/bin/env bash
# Regenerate the golden corpus and diff it against the committed baseline.
# Exit 0 == the engine still produces byte-identical oracle output (refactor is behavior-neutral).
# Exit 1 == drift; the diff is printed. Re-baseline with `./verify.sh --update` only for
# an intentional behavior change.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CLI_PROJ="$REPO_ROOT/ChessersEngine.Cli/ChessersEngine.Cli.csproj"

# Canonical generation parameters. Any change here invalidates the baseline.
SEED=1
LEVEL=2
GEN_ARGS=(--all-scenarios --ai-vs-ai --seed "$SEED" --level "$LEVEL" --quiet)

export PATH="$HOME/.dotnet:$PATH"

UPDATE=0
[[ "${1:-}" == "--update" ]] && UPDATE=1

if [[ "$UPDATE" == "1" ]]; then
  dotnet run --project "$CLI_PROJ" -c Release -- "${GEN_ARGS[@]}" --out "$SCRIPT_DIR"
  echo "Baseline updated in $SCRIPT_DIR"
  exit 0
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
dotnet run --project "$CLI_PROJ" -c Release -- "${GEN_ARGS[@]}" --out "$TMP" >/dev/null

# Compare only the JSON games; ignore this script and the README that live alongside them.
if diff -rq --exclude='*.sh' --exclude='*.md' "$SCRIPT_DIR" "$TMP"; then
  echo "oracle corpus: IDENTICAL ✔ (seed=$SEED level=$LEVEL)"
else
  echo "oracle corpus: DRIFT ✗ — engine output changed. Re-baseline only if intentional." >&2
  exit 1
fi
