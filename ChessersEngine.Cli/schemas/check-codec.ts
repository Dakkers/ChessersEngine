// Validate a codec dump and enforce that its version markers are in sync.
// Mirrors check-oracle.ts for the `codec.vN.schema.json` sibling artifact.
//
// Usage:
//   tsx check-codec.ts <schemas_dir> <codec.json> [more.json ...]

import { runMain } from "./check-common.ts";
import { codecSchemas } from "./schemas.ts";

const usage = [
  "Validate codec dumps and enforce version-marker sync.",
  "",
  "Usage:",
  "  tsx check-codec.ts <schemas_dir> <codec.json> [more.json ...]",
].join("\n");

process.exit(
  runMain(process.argv.slice(2), {
    family: "codec",
    uriRe: /codec\.v(\d+)\.schema\.json$/,
    schemaByVersion: codecSchemas,
  }, usage)
);
