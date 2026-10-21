import { afterEach, expect, it } from "vitest";
import { spawnSync } from "node:child_process";
import { existsSync, mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const generator = fileURLToPath(new URL("../../scripts/create-module.mjs", import.meta.url));
const temporary: string[] = [];
afterEach(() => { for (const directory of temporary.splice(0)) rmSync(directory, { recursive: true, force: true }); });

function generate(key: string, version: string) {
  const directory = mkdtempSync(join(tmpdir(), "elsa-template-version-"));
  temporary.push(directory);
  const output = join(directory, "module");
  return { output, result: spawnSync(process.execPath, [generator, output, `--${key}`, version], { encoding: "utf8" }) };
}

for (const key of ["studio-version", "npm-version"]) {
  it.each(["03.11.0", "3.011.0", "3.11.00", "3.11.0-alpha..1", "3.11.0-preview.01", "3.11.0-", "3.11.0-rc.", "3.11.0-.alpha"])(`${key} rejects invalid SemVer %s before writing output`, version => {
    const { output, result } = generate(key, version);
    expect(result.status).not.toBe(0);
    expect(result.stderr).toContain(`Invalid ${key}`);
    expect(existsSync(output)).toBe(false);
  });
  it.each(["3.11.0", "3.11.0-preview.1", "3.11.0-0", "3.11.0-alpha-1.01a"])(`${key} generates a module for valid SemVer %s`, version => {
    const { output, result } = generate(key, version);
    expect(result.status, result.stderr).toBe(0);
    expect(existsSync(join(output, "Module"))).toBe(true);
  });
}
