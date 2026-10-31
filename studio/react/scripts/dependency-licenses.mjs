import { execFileSync } from "node:child_process";
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

// Production dependencies only: build tools are not redistributed with the SPA.
// New licenses require review rather than silently joining this allowlist.
const allowed = new Set(["MIT", "Apache-2.0", "ISC", "BSD-2-Clause", "BSD-3-Clause", "0BSD"]);
// These published artifacts omit their upstream LICENSE files. Pin both package
// versions and the reviewed upstream notices; upgrades must reconcile the omission.
const upstreamNotices = new Map([
  ["@uiw/codemirror-extensions-basic-setup@4.25.12", "uiw-codemirror.txt"],
  ["@uiw/react-codemirror@4.25.12", "uiw-codemirror.txt"],
  ["react-remove-scroll-bar@2.3.8", "react-remove-scroll-bar.txt"],
]);
const pnpm = process.platform === "win32" ? "pnpm.cmd" : "pnpm";
const args = ["licenses", "list", "--prod", "--json"];
// pnpm supplies its JS entrypoint in scripts, including on Windows where a .cmd
// shim cannot be launched directly by execFileSync.
const report = JSON.parse(execFileSync(process.env.npm_execpath ? process.execPath : pnpm,
  process.env.npm_execpath ? [process.env.npm_execpath, ...args] : args, { encoding: "utf8" }));
const notices = [];
let packages = 0;
for (const [license, entries] of Object.entries(report)) {
  if (!allowed.has(license)) throw new Error(`Dependency license requires review: ${license}`);
  for (const entry of entries) {
    const directory = entry.paths[0];
    const files = readdirSync(directory).filter(name => /^(licen[cs]e|copying|notice)(\..*)?$/i.test(name));
    const fallback = entry.versions.length === 1 ? upstreamNotices.get(`${entry.name}@${entry.versions[0]}`) : undefined;
    if (!files.length && !fallback) throw new Error(`No license notice found for ${entry.name}`);
    notices.push(`${entry.name} ${entry.versions.join(", ")} (${license})\n` +
      (files.length ? files.map(name => readFileSync(join(directory, name), "utf8")).join("\n")
        : readFileSync(join("licenses", fallback), "utf8")));
    packages++;
  }
}
if (packages === 0) throw new Error("No production dependency licenses were scanned");
// The vendored shadcn stylesheet carries its MIT notice in the distributed CSS.
writeFileSync("dist/THIRD-PARTY-NOTICES.txt", notices.join("\n\n" + "=".repeat(72) + "\n\n"));
console.log(`Verified ${packages} production dependency licenses and retained their notices`);
