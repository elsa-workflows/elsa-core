import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const args = process.argv.slice(2);
const usage = "node scripts/create-module.mjs OUTPUT [--name Weather] [--company Acme] [--studio-version 3.11.0] [--npm-version 3.11.0]";
if (args.length === 0 || args.includes("--help")) { console.log(usage); process.exit(args.includes("--help") ? 0 : 1); }
const output = resolve(args.shift());
const options = { name: "Weather", company: "Acme", "studio-version": "3.11.0", "npm-version": "3.11.0" };
for (let index = 0; index < args.length; index += 2) {
  const key = args[index].replace(/^--/, "");
  if (!Object.hasOwn(options, key) || !args[index + 1]) throw new Error(usage);
  options[key] = args[index + 1];
}
if (!/^[A-Z][A-Za-z0-9]*$/.test(options.name) || !/^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z][A-Za-z0-9]*)*$/.test(options.company))
  throw new Error("Use a PascalCase module name and a dot-separated C# company namespace.");
const reserved = new Set("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while".split(" "));
if (options.company.split(".").some(segment => reserved.has(segment))) throw new Error("Company namespace segments cannot be reserved C# keywords.");
for (const key of ["studio-version", "npm-version"]) if (!/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(options[key])) throw new Error(`Invalid ${key}`);
if (existsSync(output) && readdirSync(output).length !== 0) throw new Error("Output must be a new or empty directory; existing files are never overwritten.");
const kebab = options.name.replace(/([a-z0-9])([A-Z])/g, "$1-$2").toLowerCase();
const companyId = options.company.toLowerCase().replaceAll(".", "-");
const replacements = { __NAME__: options.name, __COMPANY__: options.company, __PACKAGE_ID__: `${options.company}.ReactStudio.${options.name}`,
  __MODULE_ID__: `${companyId}-${kebab}`, __NPM_NAME__: `@${companyId}/react-studio-${kebab}-client`, __STUDIO_VERSION__: options["studio-version"], __NPM_VERSION__: options["npm-version"] };
const template = fileURLToPath(new URL("../samples/module-template/", import.meta.url));
function copy(directory, relative = "") {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(relative, entry.name);
    if (entry.isDirectory()) { copy(join(directory, entry.name), path); continue; }
    let content = readFileSync(join(directory, entry.name), "utf8");
    for (const [token, value] of Object.entries(replacements)) content = content.replaceAll(token, value);
    const target = join(output, path.replace(/\.template$/, ""));
    mkdirSync(dirname(target), { recursive: true });
    writeFileSync(target, content, { flag: "wx" });
  }
}
copy(template);
console.log(`Created ${replacements.__PACKAGE_ID__} in ${output}`);
