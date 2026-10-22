import { chromium, expect } from "@playwright/test";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const origin = `http://localhost:${process.env.ELSA_STUDIO_PORT ?? "5201"}`;
const api = `http://localhost:${process.env.ELSA_SERVER_PORT ?? "5001"}/elsa/api`;
const deadline = Date.now() + 90_000;
let ready = false;
while (!ready && Date.now() < deadline) {
  try {
    const response = await fetch(`${api}/identity/login`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: "admin", password: "password" }),
      signal: AbortSignal.timeout(2000),
    });
    ready = response.status === 200;
    await response.arrayBuffer();
  } catch { /* The reference backend may still be applying migrations. */ }
  if (!ready) await new Promise(resolve => setTimeout(resolve, 500));
}
if (!ready) throw new Error("Compose reference backend did not accept its Development account");

const studioDeadline = Date.now() + 60_000;
let config;
let studioFailure;
while (!config && Date.now() < studioDeadline) {
  try {
    const response = await fetch(`${origin}/studio-preview/studio-config`, { signal: AbortSignal.timeout(2000) });
    if (!response.ok) throw new Error(`Studio runtime configuration failed: ${response.status}`);
    config = await response.json();
  } catch (error) { studioFailure = error; }
  if (!config) await new Promise(resolve => setTimeout(resolve, 500));
}
if (!config) throw new Error("Compose Studio did not become ready", { cause: studioFailure });
if (config.servers[0].url !== api || config.auth.provider !== "elsa-identity" || !config.title.includes("Preview"))
  throw new Error("Compose STUDIO_* overrides were not applied");

for (const script of ["test-settings-roundtrip.mjs", "test-bpmn-roundtrip.mjs"]) {
  execFileSync(process.execPath, [fileURLToPath(new URL(script, import.meta.url))], {
    env: { ...process.env, ELSA_TEST_API_URL: api, ELSA_TEST_USERNAME: "admin", ELSA_TEST_PASSWORD: "password" },
    stdio: "inherit", timeout: 60_000,
  });
}

async function apiLogin() {
  const login = await fetch(`${api}/identity/login`, {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "admin", password: "password" }),
  });
  if (!login.ok) throw new Error(`login returned ${login.status}`);
  const tokens = await login.json();
  if (!tokens.accessToken) throw new Error("login returned no access token");
  return tokens.accessToken;
}

function hasWriteLine(root) {
  if (!root) return false;
  if (String(root.type ?? "").includes("WriteLine")) return true;
  return (root.activities ?? []).some(activity => String(activity.type ?? "").includes("WriteLine") || hasWriteLine(activity));
}

async function waitForFinishedInstance(instanceId, token) {
  const deadline = Date.now() + 30_000;
  let last;
  while (Date.now() < deadline) {
    const response = await fetch(`${api}/workflow-instances/${encodeURIComponent(instanceId)}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) throw new Error(`GET workflow-instances/${instanceId} returned ${response.status}`);
    last = await response.json();
    if (last.subStatus === "Faulted") throw new Error(`Instance ${instanceId} faulted`);
    if (last.subStatus === "Finished") return last;
    await new Promise(resolve => setTimeout(resolve, 250));
  }
  throw new Error(`Instance ${instanceId} did not finish; last subStatus=${last?.subStatus}`);
}

const browser = await chromium.launch(process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
  ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH } : {});
try {
  const page = await browser.newPage();
  await page.goto(`${origin}/studio-preview/login`);
  await page.getByLabel("Username", { exact: true }).fill("admin");
  await page.getByLabel("Password", { exact: true }).fill("password");
  const login = page.waitForResponse(response => response.url() === `${api}/identity/login` && response.request().method() === "POST");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  if ((await login).status() !== 200) throw new Error("Browser sign-in failed against the Compose backend");
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  const definitions = page.waitForResponse(response => response.url().startsWith(`${api}/workflow-definitions?`) && response.request().method() === "GET");
  await page.goto(`${origin}/studio-preview/workflows/definitions`);
  if ((await definitions).status() !== 200) throw new Error("Authenticated definition listing failed");
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();

  const name = `CoreFlow ${Date.now()}`;
  let definitionId;
  try {
    await page.getByRole("button", { name: "Create workflow", exact: true }).click();
    const create = page.getByRole("dialog", { name: "Create workflow", exact: true });
    await create.getByLabel("Name").fill(name);
    await create.getByRole("radio", { name: /Sequence/ }).click();
    await create.getByRole("button", { name: "Create", exact: true }).click();
    await expect(page).toHaveURL(/\/studio-preview\/workflows\/definitions\/[^/]+\/edit$/);
    definitionId = new URL(page.url()).pathname.split("/").at(-2);
    await expect(page.getByText("Sequence · runs top to bottom")).toBeVisible();

    await page.getByRole("button", { name: "Add step", exact: true }).click();
    const palette = page.getByRole("dialog", { name: "Add activity", exact: true });
    await palette.locator("input").fill("Write Line");
    await page.getByRole("option").filter({ hasText: /Write Line/i }).first().click();
    await expect(page.getByText("WriteLine1")).toBeVisible();
    await expect(page.getByText("Saved", { exact: true })).toBeVisible({ timeout: 15_000 });

    const token = await apiLogin();
    const saved = await fetch(`${api}/workflow-definitions/by-definition-id/${encodeURIComponent(definitionId)}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!saved.ok) throw new Error(`GET saved definition returned ${saved.status}`);
    const payload = await saved.json();
    if (payload.name !== name) throw new Error(`saved name was ${JSON.stringify(payload.name)}, expected ${JSON.stringify(name)}`);
    if (!hasWriteLine(payload.root)) throw new Error("saved definition root.activities does not contain a WriteLine");

    await page.getByRole("button", { name: "Publish", exact: true }).click();
    await expect(page.getByText(/Published version/)).toBeVisible();

    await page.getByRole("button", { name: "Run", exact: true }).click();
    const run = page.getByRole("dialog").filter({ hasText: "Dispatches the published version" });
    await run.getByRole("button", { name: "Run", exact: true }).click();
    await expect(page.getByText("Workflow started", { exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Open", exact: true }).click();
    await expect(page).toHaveURL(/\/studio-preview\/workflows\/instances\/[^/]+\/view$/);
    const instanceId = new URL(page.url()).pathname.split("/").at(-2);
    await waitForFinishedInstance(instanceId, token);
    const journal = page.getByRole("tree", { name: "Workflow journal" });
    await expect(journal).toBeVisible();
    const step = page.getByRole("treeitem").filter({ hasText: /WriteLine/ });
    await expect(step).toBeVisible();
    await step.click();
    await expect(journal.getByText("Completed", { exact: true }).first()).toBeVisible();
    await expect(journal.getByText("Faulted", { exact: true })).toHaveCount(0);

    await page.goto(`${origin}/studio-preview/settings`);
    await expect(page.getByRole("heading", { name: "Settings", exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Open Appearance", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Appearance", exact: true })).toBeVisible();
  } finally {
    if (definitionId) {
      try {
        const accessToken = await apiLogin();
        const del = await fetch(`${api}/workflow-definitions/${encodeURIComponent(definitionId)}`, {
          method: "DELETE", headers: { Authorization: `Bearer ${accessToken}` },
        });
        if (!del.ok) throw new Error(`delete returned ${del.status}`);
      } catch (error) {
        console.warn(`WARN: could not delete workflow definition ${definitionId}: ${error}`);
      }
    }
  }

  console.log("PASS: paired containers apply runtime config, sign in, list, create, edit, publish, run, journal and settings");
} finally {
  await browser.close();
}
