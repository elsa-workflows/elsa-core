import { execFileSync } from "node:child_process";
import { cpSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";

import { readBundle } from "./npm-release.mjs";

const root = process.cwd();
const args = process.argv.slice(2);
const releasedDirectory = args.includes("--npm-feed") ? resolve(args[args.indexOf("--npm-feed") + 1]) : undefined;
const released = releasedDirectory ? readBundle(releasedDirectory) : undefined;
const temporary = mkdtempSync(join(tmpdir(), "elsa-react-studio-consumer-"));
const pnpm = process.platform === "win32" ? "pnpm.cmd" : "pnpm";
function run(args, cwd) {
  execFileSync(process.env.npm_execpath ? process.execPath : pnpm,
    process.env.npm_execpath ? [process.env.npm_execpath, ...args] : args,
    { cwd, stdio: "inherit" });
}

try {
  const dependencies = {};
  for (const name of ["api-client", "sdk", "auth", "ui"]) {
    const directory = join(root, "packages", name);
    const manifest = JSON.parse(readFileSync(join(directory, "package.json"), "utf8"));
    if (!released) run(["pack", "--pack-destination", temporary], directory);
    const feed = releasedDirectory ?? temporary;
    const archive = readdirSync(feed).find(file => file === `${manifest.name.replace("@", "").replace("/", "-")}-${released?.version ?? manifest.version}.tgz`);
    if (!archive) throw new Error(`Missing packed artifact for ${manifest.name}`);
    dependencies[manifest.name] = `file:${join(feed, archive)}`;
  }
  const installed = JSON.parse(readFileSync(join(root, "package.json"), "utf8"));
  const reactTypes = JSON.parse(readFileSync(join(root, "node_modules/@types/react/package.json"), "utf8"));
  const consumer = join(temporary, "consumer");
  cpSync(join(root, "samples/weather-module"), consumer, { recursive: true });
  writeFileSync(join(consumer, "package.json"), JSON.stringify({
    private: true, type: "module", dependencies: {
      ...dependencies, react: installed.dependencies.react, "react-dom": installed.dependencies["react-dom"],
      "lucide-react": installed.dependencies["lucide-react"], "@types/react": reactTypes.version,
    }, devDependencies: { vite: installed.devDependencies.vite,
      "@tailwindcss/vite": installed.devDependencies["@tailwindcss/vite"], tailwindcss: installed.devDependencies.tailwindcss,
    }, pnpm: { overrides: dependencies },
  }));
  writeFileSync(join(consumer, "tsconfig.json"), JSON.stringify({ compilerOptions: {
    target: "ES2022", module: "NodeNext", moduleResolution: "NodeNext", strict: true,
    jsx: "react-jsx", outDir: "dist", skipLibCheck: false,
  }, include: ["entry.tsx"] }));
  run(["install", "--ignore-scripts"], consumer);
  execFileSync(process.execPath, [resolve("node_modules/typescript/bin/tsc"), "-p", "tsconfig.json"], { cwd: consumer, stdio: "inherit" });
  writeFileSync(join(consumer, "smoke.mjs"), `
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { StudioRegistry } from "@elsa-workflows/react-studio-sdk/host";
import { AuthProvider, createAuthStrategy } from "@elsa-workflows/react-studio-auth";
import { authConfigSchema, serverConfigSchema } from "@elsa-workflows/react-studio-sdk/config";
import { Weather, register } from "./dist/entry.js";
const registry = new StudioRegistry();
const activated = await registry.load({ id: "weather", version: "1.0.0", hostApiVersion: 1, sdkApiVersion: 1, entry: "./dist/entry.js" }, async () => ({ register }));
if (!activated || registry.slots("headerItems").length !== 1) throw new Error("Packed module did not register");
const strategy = createAuthStrategy(authConfigSchema.parse({provider:"none"}), serverConfigSchema.parse({url:"https://example.invalid/api"}));
const html = renderToStaticMarkup(createElement(AuthProvider, {strategy}, createElement(Weather)));
if (!html.includes("Weather for") || !html.includes('data-slot="button"')) throw new Error("Shared auth/UI render failed");
registry.remove("weather");
if (registry.slots("headerItems").length) throw new Error("Packed module did not unregister");
`);
  execFileSync(process.execPath, ["smoke.mjs"], { cwd: consumer, stdio: "inherit" });
  writeFileSync(join(consumer, "styles.js"), 'import "@elsa-workflows/react-studio-ui/styles.css";\nimport "@elsa-workflows/react-studio-ui/tokens.css";\n');
  writeFileSync(join(consumer, "index.html"), '<script type="module" src="/styles.js"></script>');
  writeFileSync(join(consumer, "vite.config.mjs"), 'import tailwindcss from "@tailwindcss/vite";\nexport default { plugins: [tailwindcss()], build: { outDir: "style-dist" } };\n');
  run(["exec", "vite", "build"], consumer);
  const cssFiles = readdirSync(join(consumer, "style-dist/assets")).filter(name => name.endsWith(".css"));
  const css = cssFiles.map(name => readFileSync(join(consumer, "style-dist/assets", name), "utf8")).join("\n");
  if (!css.includes("--background") || !css.includes(".inline-flex")) throw new Error("Packed UI stylesheet did not build its theme and component utilities");
  console.log("PASS: external module compiled/rendered and exported UI styles built using four packed public packages");
} finally {
  rmSync(temporary, { recursive: true, force: true });
}
