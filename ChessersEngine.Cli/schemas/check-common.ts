// Shared logic for the oracle/codec schema checks. For each file it enforces that:
//   1. `$schema` ends with `<family>.vN.schema.json`,
//   2. the machine-readable `schemaVersion` field equals that N,
//   3. the referenced `<family>.vN.schema.json` file exists under the schemas dir, and
//   4. the file validates against the Zod schema registered for version N.

import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { z } from "zod";

export interface CheckConfig {
  family: "oracle" | "codec";
  uriRe: RegExp; // one capture group = the version integer
  schemaByVersion: Record<number, z.ZodType>;
}

export function checkFile(filePath: string, schemasDir: string, cfg: CheckConfig): string[] {
  let doc: unknown;
  try {
    doc = JSON.parse(readFileSync(filePath, "utf8"));
  } catch (e) {
    return [`${filePath}: could not read/parse JSON: ${(e as Error).message}`];
  }

  const record = doc as Record<string, unknown>;
  const schemaUri = record?.$schema;
  if (typeof schemaUri !== "string") {
    return [`${filePath}: missing or non-string $schema`];
  }

  const m = cfg.uriRe.exec(schemaUri);
  if (!m) {
    return [
      `${filePath}: $schema must end with ${cfg.family}.vN.schema.json (got ${JSON.stringify(schemaUri)})`,
    ];
  }
  const uriVersion = Number(m[1]);

  const errs: string[] = [];

  if (record.schemaVersion !== uriVersion) {
    errs.push(
      `${filePath}: schemaVersion=${JSON.stringify(record.schemaVersion)} is out of sync with ` +
        `$schema (v${uriVersion}); the two must match`,
    );
  }

  const schemaFile = join(schemasDir, `${cfg.family}.v${uriVersion}.schema.json`);
  if (!existsSync(schemaFile)) {
    errs.push(`${filePath}: $schema references v${uriVersion} but ${schemaFile} is missing`);
    return errs;
  }

  const zodSchema = cfg.schemaByVersion[uriVersion];
  if (!zodSchema) {
    errs.push(`${filePath}: no Zod validator registered for ${cfg.family} v${uriVersion}`);
    return errs;
  }

  const result = zodSchema.safeParse(doc);
  if (!result.success) {
    const issues = result.error.issues.toSorted((a, b) =>
      a.path.join("/").localeCompare(b.path.join("/")),
    );
    for (const issue of issues) {
      errs.push(`${filePath}: schema violation at /${issue.path.join("/")}: ${issue.message}`);
    }
  }

  return errs;
}

export function runMain(argv: string[], cfg: CheckConfig, usage: string): number {
  if (argv.length < 2) {
    process.stderr.write(usage + "\n");
    return 2;
  }

  const schemasDir = argv[0];
  const files = argv.slice(1);

  const allErrs = files.flatMap((f) => checkFile(f, schemasDir, cfg));
  const label = cfg.family === "oracle" ? "Oracle" : "Codec";

  if (allErrs.length) {
    process.stdout.write(`${label} schema check FAILED:\n`);
    for (const e of allErrs) process.stdout.write(`  - ${e}\n`);
    return 1;
  }

  process.stdout.write(
    `${label} schema check OK: ${files.length} file(s) valid and version-synced.\n`,
  );
  return 0;
}
