import { expect, test, type Page } from "@playwright/test";

const api = "http://127.0.0.1:5001/elsa/api";
const claimType = "http://schemas.microsoft.com/identity/claims/tenantid";
const tenants = [{ id: "", name: "Default tenant" }, { id: "a", name: "Tenant A" }, { id: "b", name: "Tenant B" }];

async function fixture(page: Page, mode: "selection" | "claims" = "selection", tenantClaim?: string, multipleServers = false) {
  if (mode === "claims") {
    const jwt = "stub." + Buffer.from(JSON.stringify({ sub: "alice", name: "Alice", exp: Math.floor(Date.now() / 1000) + 3600, ...(tenantClaim !== undefined ? { [claimType]: tenantClaim } : {}) })).toString("base64url") + ".stub";
    await page.addInitScript(({ api, jwt }) => {
      const key = "elsa-studio:auth:" + encodeURIComponent(api);
      if (!localStorage.getItem(key)) localStorage.setItem(key, JSON.stringify({ accessToken: jwt }));
    }, { api, jwt });
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

test("a dirty server switch uses one discard prompt and one context navigation", async ({ page }) => {
  await fixture(page, "selection", undefined, true);
  await page.route("**/workflow-definitions", async route => {
    if (route.request().method() === "POST") await route.abort(); else await route.fallback();
  });
  await page.goto("/studio-preview/workflows/definitions/same/edit");
  const panel = page.getByRole("tabpanel", { name: "Properties", exact: true });
  await panel.locator("label").filter({ hasText: /^Name[*]?$/ }).locator("..").getByRole("textbox").fill("Unsaved server A");
  const dialogs: string[] = [];
  page.on("dialog", dialog => { dialogs.push(dialog.type()); void dialog.accept(); });
  await page.getByLabel("Switch environment").click();
  await page.getByRole("menuitem").filter({ has: page.getByText("Server B", { exact: true }) }).click();
  await expect(page).toHaveURL(/\/studio-preview\/$/);
  await expect(page.getByLabel("Switch environment")).toContainText("Server B");
  expect(dialogs).toEqual(["confirm"]);
});

test("a public query provider survives StrictMode replay and disposes only its unmounted scope", async ({ page }) => {
  await fixture(page);
  let requests = 0;
  await page.route(`${api}/query-lifetime`, route => route.fulfill({ json: { value: `Response ${++requests}` } }));
  let originalEntry = "";
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch();
    const manifest = await response.json();
    const module = manifest.modules.find((item: { id: string }) => item.id === "workflow-definitions");
    originalEntry = module.entry;
    module.entry = "/query-lifetime-fixture.js";
    await route.fulfill({ json: manifest });
  });
  await page.route("**/query-lifetime-fixture.js", route => route.fulfill({ contentType: "text/javascript", body: `
    import { createElement as h, StrictMode, useEffect, useState } from "react";
    import { useQuery, useQueryClient } from "@tanstack/react-query";
    import { ElsaApiProvider, useElsaClient } from "@elsa-workflows/react-studio-api-client/react";
    import { createElsaClient } from "@elsa-workflows/react-studio-api-client";
    import { register as registerDefinitions } from ${JSON.stringify(originalEntry)};
    const client = createElsaClient({baseUrl:${JSON.stringify(api)},getAuthHeaders:()=>({})});
    function Value() {
      const api = useElsaClient();
      const cache = useQueryClient();
      const query = useQuery({queryKey:["query-lifetime"],queryFn:({signal})=>api.http.get("query-lifetime",{signal}),staleTime:Infinity});
      useEffect(()=>{window.lifetimeCaches ??= [];if(!window.lifetimeCaches.includes(cache))window.lifetimeCaches.push(cache);},[cache]);
      return h("output",{"data-testid":"query-value"},query.data?.value ?? "Loading");
    }
    function Fixture() {
      const [visible,setVisible] = useState(true);
      const [scope,setScope] = useState(0);
      return h("section",null,h("button",{onClick:()=>setVisible(v=>!v)},"Toggle queries"),h("button",{onClick:()=>setScope(s=>s+1)},"Replace scope"),
        visible && h(StrictMode,null,h(ElsaApiProvider,{client,scope:String(scope)},h(Value))));
    }
    export function register(api) {registerDefinitions(api);api.contribute("routes",{id:"query-lifetime",path:"/query-lifetime",component:Fixture});}
  ` }));
  await page.goto("/studio-preview/query-lifetime");
  await expect(page.getByTestId("query-value")).toHaveText(/^Response \d+$/);
  const first = await page.getByTestId("query-value").textContent();
  await page.getByRole("button", { name: "Replace scope", exact: true }).click();
  await expect(page.getByTestId("query-value")).toHaveText(/^Response \d+$/);
  await expect(page.getByTestId("query-value")).not.toHaveText(first!);
  const cacheSizes = () => page.evaluate(() => (window as Window & { lifetimeCaches?: { getQueryCache(): { getAll(): unknown[] } }[] }).lifetimeCaches?.map(cache => cache.getQueryCache().getAll().length));
  await expect.poll(cacheSizes).toEqual([0, 1]);
  await page.getByRole("button", { name: "Toggle queries", exact: true }).click();
  await expect.poll(cacheSizes).toEqual([0, 0]);
});

test("a missing claim between authenticated tenants cannot revive another tenant's module dialog state", async ({ page }) => {
  await fixture(page, "claims", "a");
  await page.route(`${api}/identity/login`, async route => {
    const tenant = route.request().postDataJSON().username;
    const claims = { sub: "alice", name: "Alice", exp: Math.floor(Date.now() / 1000) + 3600, ...(tenant === "missing" ? {} : { [claimType]: tenant }) };
    const jwt = "stub." + Buffer.from(JSON.stringify(claims)).toString("base64url") + ".stub";
    await route.fulfill({ json: { isAuthenticated: true, accessToken: jwt } });
  });
  let originalEntry = "";
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch();
    const manifest = await response.json();
    const module = manifest.modules.find((item: { id: string }) => item.id === "workflow-definitions");
    originalEntry = module.entry;
    module.entry = "/retained-dialog-fixture.js";
    await route.fulfill({ json: manifest });
  });
  await page.route("**/retained-dialog-fixture.js", route => route.fulfill({ contentType: "text/javascript", body: `
    import { createElement as h, useState } from "react";
    import { useAuth } from "@elsa-workflows/react-studio-auth";
    import { useTenant } from "@elsa-workflows/react-studio-sdk/tenancy";
    import { register as registerDefinitions } from ${JSON.stringify(originalEntry)};
    // Module-level dialog state has the same lifetime as an independent store.
    let target;
    function Fixture() {
      const auth = useAuth();const tenant = useTenant();const [,render] = useState(0);
      window.changeTenantAccount = auth.login;
      window.retainedTenantTarget = target;
      return h("section",null,h("h1",null,"Retained dialog fixture"),
        h("button",{onClick:()=>{target=tenant.tenantId+"-instance";window.retainedTenantTarget=target;render(v=>v+1);}},"Open alteration"),
        target && h("p",{role:"status"},"Alter "+target));
    }
    export function register(api) {registerDefinitions(api);api.contribute("routes",{id:"retained-dialog",path:"/retained-dialog",component:Fixture});}
  ` }));
  await page.goto("/studio-preview/retained-dialog");
  await page.getByRole("button", { name: "Open alteration", exact: true }).click();
  await expect(page.getByRole("status")).toHaveText("Alter a-instance");
  const changeAccount = (username: string) => page.evaluate(async username => {
    await (window as Window & { changeTenantAccount?: (credentials: { username: string; password: string }) => Promise<void> }).changeTenantAccount?.({ username, password: "fixture" });
  }, username);
  await changeAccount("missing");
  await expect(page.getByRole("alert")).toContainText("does not specify a tenant");
  expect(await page.evaluate(() => (window as Window & { retainedTenantTarget?: string }).retainedTenantTarget)).toBe("a-instance");
  // The login function survives the blocked subtree, as it does during token refresh.
  await changeAccount("b").catch(error => {
    if (!/Execution context was destroyed/.test(String(error))) throw error;
  });
  await expect(page).toHaveURL(/\/studio-preview\/$/);
  expect(await page.evaluate(() => (window as Window & { retainedTenantTarget?: string }).retainedTenantTarget)).toBeUndefined();
  await page.goto("/studio-preview/retained-dialog");
  await expect(page.getByRole("heading", { name: "Retained dialog fixture" })).toBeVisible();
  await expect(page.getByText("Alter a-instance", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Open alteration", exact: true }).click();
  await expect(page.getByRole("status")).toHaveText("Alter b-instance");
});
