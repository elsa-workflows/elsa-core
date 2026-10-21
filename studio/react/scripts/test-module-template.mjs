import { execFileSync, spawn } from "node:child_process";
import { mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium, expect } from "@playwright/test";

const root = fileURLToPath(new URL("../", import.meta.url));
const args = process.argv.slice(2);
const option = name => args[args.indexOf(name) + 1];
if (!args.includes("--nuget-feed") || !args.includes("--studio-version")) throw new Error("Use --nuget-feed PATH --studio-version VERSION after packing Elsa.ReactStudio.slnf");
const feed = resolve(option("--nuget-feed"));
const version = option("--studio-version");
const temporary = mkdtempSync(join(tmpdir(), "elsa-react-module-template-"));
const generated = join(temporary, "sample");
const env = { ...process.env, NUGET_PACKAGES: join(temporary, "nuget-cache") };
for (const key of Object.keys(env)) if (key.startsWith("STUDIO_")) delete env[key];
const pnpm = process.platform === "win32" ? "pnpm.cmd" : "pnpm";
function run(command, parameters, cwd) { execFileSync(command, parameters, { cwd, env, stdio: "inherit" }); }
function npm(parameters, cwd) {
  run(process.env.npm_execpath ? process.execPath : pnpm,
    process.env.npm_execpath ? [process.env.npm_execpath, ...parameters] : parameters, cwd);
}
const xml = value => value.replaceAll("&", "&amp;").replaceAll('"', "&quot;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");
let browser;
let server;
let primaryError;

async function stopServer() {
  if (!server || server.exitCode !== null || server.signalCode !== null) return;
  const running = server;
  await new Promise((done, reject) => {
    const timer = setTimeout(() => {
      try { if (process.platform === "win32") running.kill("SIGKILL"); else process.kill(-running.pid, "SIGKILL"); } catch (error) { if (error.code !== "ESRCH") reject(error); }
    }, 5000);
    timer.unref();
    running.once("exit", () => { clearTimeout(timer); done(); });
    try { if (process.platform === "win32") running.kill("SIGTERM"); else process.kill(-running.pid, "SIGTERM"); }
    catch (error) { clearTimeout(timer); if (error.code === "ESRCH") done(); else reject(error); }
  });
}

async function startServer(enabled) {
  let output = "";
  server = spawn("dotnet", ["run", "--project", "Host/Host.csproj", "--no-build", "--no-restore", "--no-launch-profile", "-p:UseAppHost=false", "--urls", "http://127.0.0.1:0",
    `--Weather:Enabled=${enabled}`, `--ReactStudio:ConfigurationFile=${join(generated, "Host/studio.config.json")}`, "--Logging:LogLevel:Default=Information"],
  { cwd: generated, env, detached: process.platform !== "win32", stdio: ["ignore", "pipe", "pipe"] });
  return await new Promise((done, reject) => {
    const timer = setTimeout(() => reject(new Error(`Generated host did not start: ${output}`)), 60_000);
    const capture = data => {
      output = (output + data).slice(-12_000);
      const match = output.match(/Now listening on: (http:\/\/127\.0\.0\.1:\d+)/);
      if (match) { clearTimeout(timer); done(match[1]); }
    };
    server.stdout.on("data", capture);
    server.stderr.on("data", capture);
    server.once("error", error => { clearTimeout(timer); reject(error); });
    server.once("exit", code => { clearTimeout(timer); reject(new Error(`Generated host exited ${code}: ${output}`)); });
  });
}

try {
  const dependencies = {};
  const npmVersion = JSON.parse(readFileSync(join(root, "packages/sdk/package.json"), "utf8")).version;
  run(process.execPath, [join(root, "scripts/create-module.mjs"), generated, "--studio-version", version, "--npm-version", npmVersion], root);
  for (const name of ["api-client", "sdk", "auth", "ui"]) {
    const directory = join(root, "packages", name);
    const manifest = JSON.parse(readFileSync(join(directory, "package.json"), "utf8"));
    npm(["pack", "--pack-destination", temporary], directory);
    const archive = `${manifest.name.replace("@", "").replace("/", "-")}-${manifest.version}.tgz`;
    if (!readdirSync(temporary).includes(archive)) throw new Error(`Missing ${archive}`);
    dependencies[manifest.name] = `file:${join(temporary, archive)}`;
  }
  const client = join(generated, "Module/Client");
  const manifest = JSON.parse(readFileSync(join(client, "package.json"), "utf8"));
  manifest.pnpm = { overrides: dependencies };
  writeFileSync(join(client, "package.json"), JSON.stringify(manifest));
  npm(["install"], client);
  npm(["run", "build"], client);
  const localFeed = join(temporary, "feed");
  mkdirSync(localFeed);
  writeFileSync(join(generated, "NuGet.Config"), `<configuration><packageSources><clear/><add key="module" value="${xml(localFeed)}"/><add key="studio" value="${xml(feed)}"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>`);
  for (const project of ["Module/Module.csproj", "Host/Host.csproj"]) {
    if (readFileSync(join(generated, project), "utf8").includes("ProjectReference")) throw new Error("Generated projects must consume packages only");
  }
  run("dotnet", ["pack", "Module/Module.csproj", "--output", localFeed, "--configuration", "Release"], generated);
  if (!readdirSync(localFeed).includes("Acme.ReactStudio.Weather.1.0.0-preview.1.nupkg")) throw new Error("Generated module did not pack");
  run("dotnet", ["build", "Host/Host.csproj"], generated);
  browser = await chromium.launch(process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH } : {});
  const origin = await startServer(true);
  const page = await browser.newPage();
  const errors = [];
  page.on("pageerror", error => errors.push(error.message));
  let readOnly = false;
  let saved;
  const activity = { id: "child", type: "Elsa.WriteLine", version: 1, customProperties: { retained: "keep" } };
  const definition = { id: "sample-version", definitionId: "sample", name: "Sample workflow", version: 1, isLatest: true, isPublished: false, createdAt: "2026-10-21T00:00:00Z",
    variables: [], inputs: [], outputs: [], outcomes: [], options: {}, customProperties: {}, root: { id: "root", type: "Elsa.Flowchart", version: 1, activities: [activity], connections: [] } };
  await page.route("**/elsa/api/**", route => {
    const path = new URL(route.request().url()).pathname;
    let json = { items: [], totalCount: 0 };
    if (path.endsWith("descriptors/activities")) json = { items: [{ typeName: "Elsa.WriteLine", name: "WriteLine", displayName: "Write line", version: 1, kind: "Action", category: "Tests", inputs: [], outputs: [], ports: [] }] };
    else if (path.endsWith("workflow-definitions/by-definition-id/sample")) json = { ...definition, isReadonly: readOnly, root: saved?.root ?? definition.root };
    else if (path.endsWith("workflow-definitions") && route.request().method() === "POST") { saved = route.request().postDataJSON().model; json = { workflowDefinition: { ...definition, ...saved } }; }
    return route.fulfill({ json });
  });
  await page.goto(origin + "/studio-preview/");
  await expect(page.getByRole("heading", { name: "Weather", exact: true })).toBeVisible();
  await expect(page.getByText("Clear skies for Anonymous.", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Refresh forecast", exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: "Refreshes: 1" })).toBeVisible();
  const modules = await (await page.request.get(origin + "/studio-preview/studio-manifest")).json();
  const weather = modules.modules.find(module => module.id === "acme-weather");
  if (!weather) throw new Error("Packed third-party module is absent from server manifest");
  const asset = await page.request.get(origin + weather.entry);
  expect(asset.headers()["cache-control"]).toContain("immutable");
  await page.goto(origin + "/studio-preview/workflows/definitions/sample/edit");
  await page.locator('.react-flow__node[data-id="child"]').click();
  await page.getByRole("tab", { name: "Weather", exact: true }).click();
  await page.getByLabel("Weather note", { exact: true }).fill("Packaged extension note");
  await expect.poll(() => saved?.root.activities[0].customProperties["acme-weather.note"]).toBe("Packaged extension note");
  expect(saved.root.activities[0].customProperties.retained).toBe("keep");
  readOnly = true;
  await page.reload();
  await page.locator('.react-flow__node[data-id="child"]').click();
  await page.getByRole("tab", { name: "Weather", exact: true }).click();
  await expect(page.getByLabel("Weather note", { exact: true })).toHaveAttribute("readonly", "");
  expect(errors).toEqual([]);
  await page.close();
  await stopServer();
  const disabledOrigin = await startServer(false);
  const response = await fetch(disabledOrigin + "/studio-preview/studio-manifest");
  const disabled = await response.json();
  expect(disabled.modules.some(module => module.id === "acme-weather")).toBe(false);
  expect((await fetch(disabledOrigin + weather.entry)).status).toBe(404);
  console.log("PASS: generated module builds from packed npm/NuGet packages, registers a widget and editable/read-only activity tab, and disappears with its server feature");
} catch (error) {
  primaryError = error;
  throw error;
} finally {
  const cleanup = await Promise.allSettled([stopServer(), ...(browser ? [browser.close()] : [])]);
  try { rmSync(temporary, { recursive: true, force: true }); } catch (error) { cleanup.push({ status: "rejected", reason: error }); }
  const failures = cleanup.filter(result => result.status === "rejected").map(result => result.reason);
  if (failures.length) throw new AggregateError([...(primaryError ? [primaryError] : []), ...failures], "Module template verification cleanup failed", { cause: primaryError });
}
