// Mirrors check-oracle.ts for the `perf.vN.schema.json` benchmark artifact.

import { Command } from "commander";
import { runCheck } from "./check-common.ts";
import { perfSchemas } from "./schemas.ts";

new Command()
  .name("check-perf")
  .description("Validate perf reports and enforce version-marker sync.")
  .argument("<schemasDir>", "directory holding the *.vN.schema.json files")
  .argument("<files...>", "perf JSON files to validate")
  .action((schemasDir: string, files: string[]) => {
    process.exit(
      runCheck(schemasDir, files, {
        family: "perf",
        uriRe: /perf\.v(\d+)\.schema\.json$/,
        schemaByVersion: perfSchemas,
      }),
    );
  })
  .parse();
