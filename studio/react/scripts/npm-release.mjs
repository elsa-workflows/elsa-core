import { createHash } from "node:crypto";
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { join } from "node:path";

export const packageNames = ["api-client", "sdk", "auth", "ui"].map(name => `@elsa-workflows/react-studio-${name}`);
export const archiveName = (name, version) => `${name.replace("@", "").replace("/", "-")}-${version}.tgz`;
export const integrity = data => `sha512-${createHash("sha512").update(data).digest("base64")}`;

export function validateVersion(version) {
  const match = /^3\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-preview\.(0|[1-9]\d*))?$/.exec(version);
  if (!match || match[0] !== version || Number(match[1]) < 11) throw new Error("Use a React Studio 3.11+ release or preview version");
}

export function readBundle(directory, expectedSource) {
  const bundle = JSON.parse(readFileSync(join(directory, "release.json"), "utf8"));
  validateVersion(bundle.version);
  if (!/^[a-f0-9]{40}$/.test(bundle.source) || (expectedSource && bundle.source !== expectedSource))
    throw new Error("Release bundle does not match the selected source commit");
  if (JSON.stringify(bundle.packages.map(item => item.name)) !== JSON.stringify(packageNames))
    throw new Error("Release bundle must contain exactly the four public packages in dependency order");
  for (const item of bundle.packages) {
    if (item.file !== archiveName(item.name, bundle.version)) throw new Error("Unexpected package filename");
    const file = join(directory, item.file);
    if (integrity(readFileSync(file)) !== item.integrity) throw new Error(`Changed tarball: ${item.name}`);
    const manifest = JSON.parse(execFileSync("tar", ["-xOf", file, "package/package.json"], { encoding: "utf8" }));
    if (manifest.name !== item.name || manifest.version !== bundle.version || manifest.private !== false ||
        manifest.publishConfig?.access !== "public" || !manifest.description.includes("Preview") ||
        manifest.license !== "SEE LICENSE IN SOURCE-NOTICE.md" ||
        manifest.repository?.url !== "git+https://github.com/elsa-workflows/elsa-core.git")
      throw new Error(`Invalid public package metadata: ${item.name}`);
    for (const group of ["dependencies", "peerDependencies", "optionalDependencies"]) {
      for (const [name, version] of Object.entries(manifest[group] ?? {})) {
        if (version.startsWith("workspace:") || (packageNames.includes(name) && version !== bundle.version))
          throw new Error(`Non-lockstep dependency in ${item.name}: ${name}`);
      }
    }
    const notice = execFileSync("tar", ["-xOf", file, "package/SOURCE-NOTICE.md"], { encoding: "utf8" });
    if (!notice.includes("no license file")) throw new Error(`Missing source notice: ${item.name}`);
    const entries = new Set(execFileSync("tar", ["-tf", file], { encoding: "utf8" }).trim().split("\n"));
    for (const target of Object.values(manifest.exports)) {
      if (typeof target === "string" && target.endsWith(".css") && !entries.has(`package/${target.replace(/^\.\//, "")}`))
        throw new Error(`Missing exported stylesheet in ${item.name}: ${target}`);
    }
  }
  return bundle;
}

/** Preflight every package before the first irreversible publish. */
export function publicationPlan(bundle, metadata) {
  const preview = bundle.version.includes("-preview.");
  return bundle.packages.map((item, index) => {
    const versions = metadata[index]?.versions ?? {};
    if (!preview && !Object.keys(versions).some(version => version.startsWith(`${bundle.version}-preview.`) &&
        /^\d+$/.test(version.slice(`${bundle.version}-preview.`.length))))
      throw new Error(`Publish a ${bundle.version} preview first: ${item.name}`);
    const existing = versions[bundle.version];
    if (existing && existing.dist?.integrity !== item.integrity)
      throw new Error(`Registry version already exists with different bytes: ${item.name}`);
    const tag = preview ? "preview" : "latest";
    const current = metadata[index]?.["dist-tags"]?.[tag];
    if (!existing && current && newerVersion(current, bundle.version))
      throw new Error(`Refuse to move ${tag} backwards from ${current}: ${item.name}`);
    return { ...item, skip: !!existing, tag };
  });
}

function newerVersion(left, right) {
  const parts = value => /^(\d+)\.(\d+)\.(\d+)(?:-preview\.(\d+))?$/.exec(value);
  const a = parts(left), b = parts(right);
  if (!a || !b) throw new Error("Cannot compare the registry dist-tag with the selected release");
  for (let index = 1; index <= 3; index++) {
    if (BigInt(a[index]) !== BigInt(b[index])) return BigInt(a[index]) > BigInt(b[index]);
  }
  if (a[4] === undefined || b[4] === undefined) return a[4] === undefined && b[4] !== undefined;
  return BigInt(a[4]) > BigInt(b[4]);
}
