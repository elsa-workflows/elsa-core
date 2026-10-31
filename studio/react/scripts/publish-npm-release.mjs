import { execFileSync } from "node:child_process";
import { resolve, join } from "node:path";
import { publicationPlan, readBundle } from "./npm-release.mjs";

const [directory, mode] = process.argv.slice(2);
if (!directory || (mode && mode !== "--dry-run")) throw new Error("Usage: node scripts/publish-npm-release.mjs DIRECTORY [--dry-run]");
const output = resolve(directory);
const dryRun = mode === "--dry-run";
if (!dryRun && (process.env.GITHUB_REPOSITORY !== "elsa-workflows/elsa-core" ||
    process.env.REACT_STUDIO_PUBLISH_ENABLED !== "true" || process.env.GITHUB_ACTIONS !== "true"))
  throw new Error("Publication requires the Release-owned GitHub workflow and explicit cutover enablement");
const bundle = readBundle(output, process.env.GITHUB_SHA);
if (!dryRun && process.env.GITHUB_REF !== `refs/tags/${bundle.version}`) throw new Error("The selected release tag must match the bundle");
const metadata = await Promise.all(bundle.packages.map(async item => {
  const response = await fetch(`https://registry.npmjs.org/${encodeURIComponent(item.name)}`, { signal: AbortSignal.timeout(30_000) });
  if (response.status === 404) return {};
  if (!response.ok) throw new Error(`Registry preflight failed for ${item.name}: ${response.status}`);
  return response.json();
}));
for (const item of publicationPlan(bundle, metadata)) {
  if (item.skip) { console.log(`Already published with identical integrity: ${item.name}@${bundle.version}`); continue; }
  execFileSync("npm", ["publish", join(output, item.file), "--access", "public", "--tag", item.tag, "--registry", "https://registry.npmjs.org",
    ...(dryRun ? ["--dry-run"] : ["--provenance"])], { stdio: "inherit" });
}
console.log(dryRun ? "PASS: npm publication dry run; no packages published" : "Published all four npm packages with provenance");
