// Validate oracle files and enforce that their version markers are in sync.
//
// Usage: node check-oracle.ts <schemasDir> <oracle.json> [more.json ...]

import { Command } from "commander";
import { runCheck } from "./check-common.ts";
import { oracleSchemas } from "./schemas.ts";

new Command()
  .name("check-oracle")
  .description("Validate oracle files and enforce version-marker sync.")
  .argument("<schemasDir>", "directory holding the *.vN.schema.json files")
  .argument("<files...>", "oracle JSON files to validate")
  .action((schemasDir: string, files: string[]) => {
    process.exit(
      runCheck(schemasDir, files, {
        family: "oracle",
        uriRe: /oracle\.v(\d+)\.schema\.json$/,
        schemaByVersion: oracleSchemas,
      }),
    );
  })
  .parse();
