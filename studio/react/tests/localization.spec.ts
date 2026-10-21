import { expect, test, type Page } from "@playwright/test";

const api = "http://127.0.0.1:5001/elsa/api";
const token = (subject: string) => "stub." + Buffer.from(JSON.stringify({ sub: subject, name: subject, exp: Math.floor(Date.now() / 1000) + 3600 })).toString("base64url") + ".stub";

async function fixture(page: Page, subject?: string) {
  const writes: { name?: string; root?: { activities?: { text?: { expression?: { value?: string } } }[] } }[] = [];
  if (subject) await page.addInitScript(({ api, jwt }) => {
    const key = "elsa-studio:auth:" + encodeURIComponent(api);
    if (!localStorage.getItem(key)) localStorage.setItem(key, JSON.stringify({ accessToken: jwt }));
  }, { api, jwt: token(subject) });
  await page.route("**/studio-config", route => route.fulfill({ json: { servers: [{ name: "Development", url: api }], auth: { provider: subject ? "elsa-identity" : "none" }, features: { settings: { enabled: true }, appearance: { enabled: true }, "user-tasks": { enabled: true }, "diagnostics-console-logs": { enabled: true }, "diagnostics-structured-logs": { enabled: true } } } }));
  const definition = { id: "version", definitionId: "localization", name: "Settings", version: 1, isLatest: true, isPublished: false, createdAt: "2026-10-21T00:00:00Z", variables: [], inputs: [], outputs: [], outcomes: [], options: {}, customProperties: {},
    root: { id: "root", type: "Elsa.Flowchart", version: 1, connections: [], activities: [{ id: "child", type: "Elsa.WriteLine", version: 1, text: { typeName: "String", expression: { type: "CSharp", value: '"Settings"' } } }] } };
  await page.route("**/elsa/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/features/installed")) { await route.fulfill({ json: { items: [
      { name: "ConsoleLogs", namespace: "Elsa.Diagnostics.ConsoleLogs.ShellFeatures", fullName: "Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogs" },
      { name: "StructuredLogs", namespace: "Elsa.Diagnostics.StructuredLogs.ShellFeatures", fullName: "Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogs" },
      { name: "UserTasks", namespace: "Elsa", fullName: "Elsa.UserTasks" },
    ] } }); return; }
    if (path.includes("scripting/javascript/type-definitions")) { await route.fulfill({ contentType: "application/x-typescript", body: "" }); return; }
    if (path.includes("/diagnostics/") && path.endsWith("/sources")) { await route.fulfill({ json: [] }); return; }
    let json: unknown = { items: [], totalCount: 0 };
    if (path.endsWith("workflow-definitions") && route.request().method() === "GET") json = { items: [definition], totalCount: 1 };
    else if (path.endsWith("workflow-definitions/by-definition-id/localization")) json = definition;
    else if (path.endsWith("workflow-definitions") && route.request().method() === "POST") { writes.push(route.request().postDataJSON().model); await route.abort(); return; }
    else if (path.endsWith("descriptors/activities")) json = { items: [{ typeName: "Elsa.WriteLine", name: "WriteLine", displayName: "Write line", version: 1, kind: "Action", category: "Tests", outputs: [], ports: [], inputs: [{ name: "Text", typeName: "String", isWrapped: true, uiHint: "singleline", defaultSyntax: "Literal" }] }] };
    await route.fulfill({ json });
  });
  return writes;
}

async function dutch(page: Page) {
  await page.getByLabel("Language", { exact: true }).click();
  await page.getByRole("option", { name: "Dutch", exact: true }).click();
  await expect(page.locator("html")).toHaveAttribute("lang", "nl");
}

test("Dutch updates shell, module metadata, accessible labels and dialogs, persists, and preserves workflow names", async ({ page }) => {
  await fixture(page);
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  await page.evaluate(() => { (window as Window & { cultureMarker?: number }).cultureMarker = 42; });
  await dutch(page);
  await expect(page.getByRole("heading", { name: "Werkstroomdefinities", exact: true })).toBeVisible();
  await expect(page.getByLabel("Taal", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Settings", exact: true })).toBeVisible();
  expect(await page.evaluate(() => (window as Window & { cultureMarker?: number }).cultureMarker)).toBe(42);
  await page.getByRole("button", { name: "Workflow maken", exact: true }).click();
  await expect(page.getByRole("dialog")).toContainText("werkstroom");
  await page.getByRole("button", { name: "Annuleren", exact: true }).click();
  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("lang", "nl");
  await expect(page.getByRole("heading", { name: "Werkstroomdefinities", exact: true })).toBeVisible();
});

test("a culture switch preserves the current unsaved workflow, selected activity and expression", async ({ page }) => {
  const writes = await fixture(page);
  await page.goto("/studio-preview/workflows/definitions/localization/edit");
  const name = page.getByRole("tabpanel", { name: "Properties", exact: true }).locator("label").filter({ hasText: /^Name[*]?$/ }).locator("..").getByRole("textbox");
  await name.fill("Settings unsaved");
  await page.locator('.react-flow__node[data-id="child"]').click();
  await page.locator(".cm-content").fill('"Dashboard unchanged"');
  const url = page.url();
  await dutch(page);
  expect(page.url()).toBe(url);
  await expect(page.locator(".cm-content")).toHaveText('"Dashboard unchanged"');
  await expect(page.locator('.react-flow__node[data-id="child"]')).toHaveClass(/selected/);
  await expect.poll(() => writes.some(model => model.name === "Settings unsaved" && model.root?.activities?.[0].text?.expression?.value === '"Dashboard unchanged"')).toBe(true);
});

test("culture preferences are separate for authenticated subjects", async ({ page }) => {
  await fixture(page, "alice");
  await page.goto("/studio-preview/workflows/definitions");
  await dutch(page);
  const setUser = (subject: string) => page.evaluate(({ api, jwt }) => localStorage.setItem("elsa-studio:auth:" + encodeURIComponent(api), JSON.stringify({ accessToken: jwt })), { api, jwt: token(subject) });
  await setUser("bob");
  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await setUser("alice");
  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("lang", "nl");
});

test("independent module resources share the culture while preserving component state and English fallback", async ({ page }) => {
  await fixture(page);
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch(); const manifest = await response.json();
    manifest.modules.push({ id: "weather", version: "1.0.0", hostApiVersion: 1, sdkApiVersion: 1, entry: "/localized-weather.js", core: true });
    await route.fulfill({ json: manifest });
  });
  await page.route("**/localized-weather.js", route => route.fulfill({ contentType: "text/javascript", body: `
    import { createElement as h, useState } from "react";
    import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
    function Weather(){const t=useTranslation("weather");const [count,setCount]=useState(0);return h("section",null,h("span",{"data-testid":"weather"},t("Weather headline",{name:"Ada"})+" "+count),h("span",{"data-testid":"fallback"},t("Fallback")),h("button",{onClick:()=>setCount(c=>c+1)},"Increment weather"));}
    export function register(api){
      api.contribute("translations",{id:"en",culture:"en",messages:{"Weather headline":"Weather for {{name}}","Fallback":"English fallback","Weather route":"Weather"}});
      api.contribute("translations",{id:"nl",culture:"nl",messages:{"Weather headline":"Het weer voor {{name}}","Weather route":"Weer","Settings":"Module only"}});
      api.contribute("headerItems",{id:"weather",component:Weather});api.contribute("navigation",{id:"weather",label:"Weather route",href:"/weather"});api.contribute("routes",{id:"weather",path:"/weather",component:Weather});
    }
  ` }));
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByTestId("weather")).toHaveText("Weather for Ada 0");
  await page.getByRole("button", { name: "Increment weather", exact: true }).click();
  await dutch(page);
  await expect(page.getByTestId("weather")).toHaveText("Het weer voor Ada 1");
  await expect(page.getByTestId("fallback")).toHaveText("English fallback");
  await expect(page.getByRole("link", { name: "Weer", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Instellingen", exact: true })).toBeVisible();
});


test("log count selectors follow live culture changes after their modules loaded", async ({ page }) => {
  await fixture(page);
  await page.goto("/studio-preview/diagnostics/console?limit=250");
  await expect(page.getByRole("combobox").filter({ hasText: "250 lines" })).toBeVisible();
  await dutch(page);
  await expect(page.getByRole("combobox").filter({ hasText: "250 regels" })).toBeVisible();
  await page.getByLabel("Taal", { exact: true }).click();
  await page.getByRole("option", { name: "Engels", exact: true }).click();
  await page.goto("/studio-preview/diagnostics/structured-logs");
  await expect(page.getByRole("combobox").filter({ hasText: "250 entries" })).toBeVisible();
  await dutch(page);
  await expect(page.getByRole("combobox").filter({ hasText: "250 regels" })).toBeVisible();
});


test("task completion notices use the culture at response time and retain the task title", async ({ page }) => {
  await fixture(page);
  let respond!: () => void;
  const completed = new Promise<void>(resolve => { respond = resolve; });
  await page.route(`${api}/user-tasks/**`, async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/claim")) {
      await completed;
      await route.fulfill({ json: { status: "Accepted" } });
    } else if (path.endsWith("/capabilities")) await route.fulfill({ json: { enabled: true, canList: true, canRead: true, canClaim: true } });
    else await route.fulfill({ json: { id: "culture", title: "Task claimed.", status: "Available", priority: 0, tags: [], allowedActions: ["claim"], revision: 1, actions: [], disclosure: { canViewHistory: false }, createdAt: "2026-10-21T00:00:00Z", updatedAt: "2026-10-21T00:00:00Z" } });
  });
  await page.goto("/studio-preview/workflows/user-tasks/culture");
  await expect(page.getByRole("heading").filter({ hasText: "Task claimed." })).toBeVisible();
  await page.getByRole("button", { name: "Claim", exact: true }).click();
  await dutch(page);
  respond();
  await expect(page.getByText("Taak geclaimd. (wordt op de achtergrond verwerkt).", { exact: true })).toBeVisible();
  await expect(page.getByRole("heading").filter({ hasText: "Task claimed." })).toBeVisible();
});

test("BPMN client refusal explanations translate while retaining server diagnostics", async ({ page }) => {
  await fixture(page);
  await page.route("**/bpmn/analyze", route => route.fulfill({ status: 422, json: { code: "bpmn.import.capability-unsupported", message: "Server-specific explanation" } }));
  await page.goto("/studio-preview/workflows/definitions");
  await dutch(page);
  await page.getByRole("button", { name: "BPMN importeren", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.locator('input[type="file"]').setInputFiles({ name: "sample.bpmn", mimeType: "application/xml", buffer: Buffer.from('<definitions xmlns="http://www.omg.org/spec/BPMN/20100524/MODEL"><process id="first"/></definitions>') });
  await expect(dialog.getByText("Deze implementatie ondersteunt niet alle BPMN-mogelijkheden die het document vereist. Het importeren is geweigerd.", { exact: true })).toBeVisible();
  await expect(dialog.getByText("Server-specific explanation", { exact: true })).toBeVisible();
});
