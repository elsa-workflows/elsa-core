import { readdirSync, readFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import ts from "typescript";

// #8845 invariant: modules consume shared public packages, never host or sibling source.
const root = resolve("dotnet/modules");
const catalog = JSON.parse(readFileSync("module-catalog.json", "utf8"));
function files(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = join(directory, entry.name);
    return entry.isDirectory() ? entry.name === "node_modules" ? [] : files(path) : /\.[cm]?tsx?$/.test(entry.name) ? [path] : [];
  });
}
let checked = 0;
for (const module of catalog) {
  const directory = resolve(module.client);
  for (const path of files(directory)) {
    for (const { fileName: specifier } of ts.preProcessFile(readFileSync(path, "utf8"), true, true).importedFiles) {
      if (specifier.startsWith("@/")) throw new Error(`${relative(root, path)} imports host source: ${specifier}`);
      if (specifier.startsWith(".")) {
        const target = resolve(dirname(path), specifier);
        const escaped = relative(directory, target);
        if (escaped === ".." || escaped.startsWith("../") || escaped.startsWith("..\\"))
          throw new Error(`${relative(root, path)} imports another module's source: ${specifier}`);
      }
    }
    checked++;
  }
}
if (!checked) throw new Error("No module source files were checked");
console.log(`PASS: ${checked} module files use public packages and module-local imports`);
