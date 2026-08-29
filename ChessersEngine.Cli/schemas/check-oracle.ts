// Validate oracle files and enforce that their version markers are in sync.
//
// Usage:
//   tsx check-oracle.ts <schemas_dir> <oracle.json> [more.json ...]

import { runMain } from "./check-common.ts";
import { oracleSchemas } from "./schemas.ts";

const usage = [
  "Validate oracle files and enforce version-marker sync.",
  "",
  "Usage:",
  "  node check-oracle.ts <schemas_dir> <oracle.json> [more.json ...]",
].join("\n");

process.exit(
  runMain(
    process.argv.slice(2),
    {
      family: "oracle",
      uriRe: /oracle\.v(\d+)\.schema\.json$/,
      schemaByVersion: oracleSchemas,
    },
    usage,
  ),
);
