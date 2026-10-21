import { execFileSync } from "node:child_process";
import { cpSync, existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { archiveName, integrity, packageNames, readBundle, validateVersion } from "./npm-release.mjs";

const root = fileURLToPath(new URL("../", import.meta.url));
const [version, destination] = process.argv.slice(2);
if (!version || !destination) throw new Error("Usage: node scripts/prepare-npm-release.mjs VERSION OUTPUT_DIRECTORY");
validateVersion(version);
try {
  execFileSync("git", ["diff", "--quiet", "HEAD", "--"], { cwd: root, stdio: "ignore" });
  const untracked = execFileSync("git", ["ls-files", "--others", "--exclude-standard", "--", "packages"], { cwd: root, encoding: "utf8" });
  if (untracked.trim()) throw new Error("Untracked package source");
} catch { throw new Error("Commit source changes before preparing release artifacts so release.json identifies their actual source"); }
const output = resolve(destination);
if (existsSync(join(output, "release.json"))) throw new Error("Choose a fresh release output directory");
mkdirSync(output, { recursive: true });
const temporary = mkdtempSync(join(tmpdir(), "elsa-npm-release-"));
const source = execFileSync("git", ["rev-parse", "HEAD"], { cwd: root, encoding: "utf8" }).trim();
const bundle = { version, source, packages: [] };
try {
  for (const name of packageNames) {
    const short = name.replace("@elsa-workflows/react-studio-", "");
    const original = join(root, "packages", short);
    const staged = join(temporary, short);
    mkdirSync(staged);
    cpSync(join(original, "dist"), join(staged, "dist"), { recursive: true });
    cpSync(join(original, "README.md"), join(staged, "README.md"));
    cpSync(join(root, "SOURCE-NOTICE.md"), join(staged, "SOURCE-NOTICE.md"));
    if (short === "ui") {
      mkdirSync(join(staged, "src"));
      for (const file of readdirSync(join(original, "src")).filter(file => file.endsWith(".css")))
        cpSync(join(original, "src", file), join(staged, "src", file));
    }
    const manifest = JSON.parse(readFileSync(join(original, "package.json"), "utf8"));
    Object.assign(manifest, { version, private: false, files: [...manifest.files, "SOURCE-NOTICE.md"],
      license: "SEE LICENSE IN SOURCE-NOTICE.md", publishConfig: { access: "public" },
      repository: { type: "git", url: "git+https://github.com/elsa-workflows/elsa-core.git", directory: `studio/react/packages/${short}` },
    });
    delete manifest.scripts;
    for (const group of ["dependencies", "peerDependencies", "optionalDependencies"]) {
      for (const dependency of Object.keys(manifest[group] ?? {})) {
        if (packageNames.includes(dependency)) manifest[group][dependency] = version;
      }
    }
    writeFileSync(join(staged, "package.json"), JSON.stringify(manifest, null, 2) + "\n");
    execFileSync("npm", ["pack", "--ignore-scripts", "--json", "--pack-destination", output], {
      cwd: staged, stdio: ["ignore", "pipe", "inherit"],
    });
    const file = archiveName(name, version);
    bundle.packages.push({ name, file, integrity: integrity(readFileSync(join(output, file))) });
  }
  writeFileSync(join(output, "release.json"), JSON.stringify(bundle, null, 2) + "\n");
  readBundle(output, source);
  console.log(`PASS: prepared four lockstep ${version} npm tarballs without changing workspace manifests (${dirname(output)})`);
} finally {
  rmSync(temporary, { recursive: true, force: true });
}
