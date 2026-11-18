import { cpSync } from "node:fs";
import { createRequire } from "node:module";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const relativeDirectory = "vendor/picomatch-build";
export const buildGlobOverride = `file:./${relativeDirectory}`;
const sourceDirectory = fileURLToPath(new URL(`../${relativeDirectory}/`, import.meta.url));

export function copyBuildGlobTooling(projectDirectory) {
  cpSync(sourceDirectory, join(projectDirectory, relativeDirectory), { recursive: true, errorOnExist: true, force: false });
}

export function assertViteUsesHardenedGlobTooling(projectDirectory) {
  const require = createRequire(join(projectDirectory, "package.json"));
  const vite = createRequire(require.resolve("vite/package.json"));
  const tinyglobby = createRequire(vite.resolve("tinyglobby"));
  const fdir = createRequire(tinyglobby.resolve("fdir"));
  for (const requester of [vite, tinyglobby, fdir]) {
    const manifest = requester("picomatch/package.json");
    if (manifest.name !== "@elsa-workflows/picomatch-build" || manifest.private !== true) {
      throw new Error(`Build tooling resolved an unexpected matcher from ${requester.resolve("picomatch")}`);
    }
  }
  console.log("Verified the standalone build uses the hardened glob dependency.");
}
