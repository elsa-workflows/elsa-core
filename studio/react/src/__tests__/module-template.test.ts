import { spawnSync } from "node:child_process";
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { expect, it } from "vitest";

const generator = fileURLToPath(new URL("../../scripts/create-module.mjs", import.meta.url));

it("refuses to overwrite an author's existing files", () => {
  const directory = mkdtempSync(join(tmpdir(), "elsa-module-generator-"));
  try {
    const path = join(directory, "existing.txt");
    writeFileSync(path, "preserve me");
    const result = spawnSync(process.execPath, [generator, directory], { encoding: "utf8" });
    expect(result.status).toBe(1);
    expect(result.stderr).toContain("existing files are never overwritten");
    expect(readFileSync(path, "utf8")).toBe("preserve me");
  } finally { rmSync(directory, { recursive: true, force: true }); }
});

it("rejects a module name that cannot safely form C# identifiers and package metadata", () => {
  const directory = mkdtempSync(join(tmpdir(), "elsa-module-generator-"));
  try {
    const result = spawnSync(process.execPath, [generator, directory, "--name", "../invalid"], { encoding: "utf8" });
    expect(result.status).toBe(1);
    expect(result.stderr).toContain("PascalCase module name");
  } finally { rmSync(directory, { recursive: true, force: true }); }
});
