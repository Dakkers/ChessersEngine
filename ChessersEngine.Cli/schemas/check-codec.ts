// Mirrors check-oracle.ts for the `codec.vN.schema.json` sibling artifact.

import { Command } from "commander";
import { runCheck } from "./check-common.ts";
import { codecSchemas } from "./schemas.ts";

new Command()
  .name("check-codec")
  .description("Validate codec dumps and enforce version-marker sync.")
  .argument("<schemasDir>", "directory holding the *.vN.schema.json files")
  .argument("<files...>", "codec JSON files to validate")
  .action((schemasDir: string, files: string[]) => {
    process.exit(
      runCheck(schemasDir, files, {
        family: "codec",
        uriRe: /codec\.v(\d+)\.schema\.json$/,
        schemaByVersion: codecSchemas,
      }),
    );
  })
  .parse();
