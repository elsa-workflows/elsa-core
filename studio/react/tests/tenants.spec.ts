import { expect, test, type Page } from "@playwright/test";

const api = "http://127.0.0.1:5001/elsa/api";
const claimType = "http://schemas.microsoft.com/identity/claims/tenantid";
const tenants = [{ id: "", name: "Default tenant" }, { id: "a", name: "Tenant A" }, { id: "b", name: "Tenant B" }];

async function fixture(page: Page, mode: "selection" | "claims" = "selection", tenantClaim?: string, multipleServers = false) {
  if (mode === "claims") {
    const jwt = "stub." + Buffer.from(JSON.stringify({ sub: "alice", name: "Alice", exp: Math.floor(Date.now() / 1000) + 3600, ...(tenantClaim !== undefined ? { [claimType]: tenantClaim } : {}) })).toString("base64url") + ".stub";
    await page.addInitScript(({ api, jwt }) => localStorage.setItem("elsa-studio:auth:" + encodeURIComponent(api), JSON.stringify({ accessToken: jwt })), { api, jwt });
  }
  await page.route("**/studio-config", route => route.fulfill({ json: { servers: [{ name: "Server A", url: api, tenantId: "a", tenancy: { mode, tenants } },
    ...(multipleServers ? [{ name: "Server B", url: "http://127.0.0.1:5002/elsa/api", tenantId: "b", tenancy: { mode, tenants } }] : [])], features: { environments: { enabled: multipleServers } }, auth: { provider: mode === "selection" ? "none" : "elsa-identity" } } }));
  const calls: { path: string; tenant: string | undefined; origin: string }[] = [];
  await page.route("**/elsa/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    const tenant = route.request().headers()["x-tenant-id"];
    calls.push({ path, tenant, origin: new URL(route.request().url()).origin });
    const id = tenant || "default";
    const definition = { id: `version-${id}`, definitionId: "same", name: `Workflow ${id}`, version: 1, isLatest: true, isPublished: false, createdAt: "2026-10-21T00:00:00Z", variables: [], inputs: [], outputs: [], outcomes: [], options: {}, customProperties: {}, root: { id: "root", type: "Elsa.Flowchart", version: 1, activities: [{ id: `${id}-child`, type: "Elsa.WriteLine", version: 1 }], connections: [] } };
    let json: unknown = { items: [], totalCount: 0 };
    if (path.endsWith("workflow-definitions/by-definition-id/same")) json = definition;
    else if (path.endsWith("workflow-definitions") && route.request().method() === "GET") json = { items: [definition], totalCount: 1 };
    else if (path.endsWith("workflow-definitions") && route.request().method() === "POST") json = { workflowDefinition: { ...definition, ...route.request().postDataJSON().model } };
    else if (path.endsWith("descriptors/activities")) json = { items: [{ typeName: "Elsa.WriteLine", name: "WriteLine", displayName: "Write line", version: 1, kind: "Action", category: "Tests", inputs: [], outputs: [], ports: [] }] };
    await route.fulfill({ json });
  });
  return calls;
}

async function select(page: Page, name: string) {
  await page.getByLabel("Switch tenant").click();
  await page.getByRole("option", { name, exact: true }).click();
}

test("switching tenants clears module state and reloads data/graphs for the same definition ID", async ({ page }) => {
  page.on("dialog", dialog => dialog.accept());
  const calls = await fixture(page);
  await page.goto("/studio-preview/workflows/definitions/same/edit");
  await expect(page.locator('.react-flow__node[data-id="a-child"]')).toBeVisible();
  await page.evaluate(() => { (window as Window & { tenantMarker?: string }).tenantMarker = "old module state"; });
  await select(page, "Tenant B");
  await expect(page).toHaveURL(/\/studio-preview\/$/);
  expect(await page.evaluate(() => (window as Window & { tenantMarker?: string }).tenantMarker)).toBeUndefined();
  await page.goto("/studio-preview/workflows/definitions/same/edit");
  await expect(page.locator('.react-flow__node[data-id="b-child"]')).toBeVisible();
  await expect(page.locator('.react-flow__node[data-id="a-child"]')).toHaveCount(0);
  await page.reload();
  await expect(page.getByLabel("Switch tenant")).toContainText("Tenant B");
  expect(calls.some(call => call.tenant === "a")).toBe(true);
  expect(calls.some(call => call.path.endsWith("workflow-definitions/by-definition-id/same") && call.tenant === "b")).toBe(true);
  await select(page, "Default tenant");
  await expect(page.getByLabel("Switch tenant")).toContainText("Default tenant");
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByRole("link", { name: "Workflow default", exact: true })).toBeVisible();
  expect(calls.some(call => call.path.endsWith("workflow-definitions") && call.tenant === "")).toBe(true);
});

test("an unsaved workflow can cancel a tenant switch before its preference is changed", async ({ page }) => {
  await fixture(page);
  // Keep any autosave in flight so dirty state cannot disappear while interacting.
  await page.route("**/workflow-definitions", async route => {
    if (route.request().method() === "POST") await route.abort(); else await route.fallback();
  });
  await page.goto("/studio-preview/workflows/definitions/same/edit");
  const panel = page.getByRole("tabpanel", { name: "Properties", exact: true });
  await panel.locator("label").filter({ hasText: /^Name[*]?$/ }).locator("..").getByRole("textbox").fill("Unsaved tenant A");
  const dialog = page.waitForEvent("dialog");
  const switching = select(page, "Tenant B");
  const confirmation = await dialog;
  expect(confirmation.message()).toBe("Switch tenant and discard unsaved changes?");
  await confirmation.dismiss();
  await switching;
  await expect(page).toHaveURL(/definitions\/same\/edit$/);
  await expect(page.getByLabel("Switch tenant")).toContainText("Tenant A");
  expect(await page.evaluate(() => Object.keys(localStorage).filter(key => key.startsWith("elsa-studio:tenant:")))).toEqual([]);
  const dialogs: string[] = [];
  page.on("dialog", dialog => { dialogs.push(dialog.type()); void dialog.accept(); });
  await select(page, "Tenant B");
  await expect(page).toHaveURL(/\/studio-preview\/$/);
  expect(dialogs).toEqual(["confirm"]);
});

test("claims mode uses the authenticated tenant and exposes no selector", async ({ page }) => {
  const calls = await fixture(page, "claims", "b");
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByRole("link", { name: "Workflow b", exact: true })).toBeVisible();
  await expect(page.getByText("Tenant: Tenant B", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Switch tenant")).toHaveCount(0);
  expect(calls.length).toBeGreaterThan(0);
  expect(calls.every(call => call.tenant === "b")).toBe(true);
});

test("missing required tenant claims block module API calls", async ({ page }) => {
  const calls = await fixture(page, "claims");
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByRole("alert")).toContainText("does not specify a tenant");
  expect(calls).toEqual([]);
});

test("tenant preferences and fresh module state follow the selected server", async ({ page }) => {
  const calls = await fixture(page, "selection", undefined, true);
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByRole("link", { name: "Workflow a", exact: true })).toBeVisible();
  await select(page, "Tenant B");
  await page.getByLabel("Switch environment").click();
  await page.getByRole("menuitem").filter({ has: page.getByText("Server B", { exact: true }) }).click();
  await expect(page.getByLabel("Switch environment")).toContainText("Server B");
  await expect(page.getByLabel("Switch tenant")).toContainText("Tenant B");
  await select(page, "Tenant A");
  await page.getByLabel("Switch environment").click();
  await page.getByRole("menuitem").filter({ has: page.getByText("Server A", { exact: true }) }).click();
  await expect(page.getByLabel("Switch environment")).toContainText("Server A");
  await expect(page.getByLabel("Switch tenant")).toContainText("Tenant B");
  expect(calls.some(call => call.origin.endsWith(":5002") && call.tenant === "a")).toBe(true);
});
