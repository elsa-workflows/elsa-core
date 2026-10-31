import { expect, test, type Locator, type Page } from "@playwright/test";
import type { WorkflowDefinition } from "../packages/api-client/src/types/workflow-definitions";

async function editorFixture(page: Page, published = false) {
  let definition: WorkflowDefinition = { id: "version-three", definitionId: "parity", name: "Parity workflow", version: 3, isLatest: true, isPublished: published, createdAt: "2026-10-21T00:00:00Z",
    variables: [{ id: "customer", name: "Customer", typeName: "String", value: "original" }], inputs: [], outputs: [], outcomes: ["Approved"], options: {}, customProperties: {},
    root: { id: "root", nodeId: "root", type: "Elsa.Flowchart", version: 1, activities: [
      { id: "alpha", nodeId: "root:alpha", type: "Elsa.WriteLine", version: 1, name: "Alpha1", metadata: { designer: { position: { x: 0, y: 0 } } } },
      { id: "beta", nodeId: "root:beta", type: "Elsa.WriteLine", version: 1, name: "Beta1", metadata: { designer: { position: { x: 280, y: 0 } } } },
    ], connections: [{ source: { activity: "alpha", port: "Done" }, target: { activity: "beta", port: "In" } }] } };
  const historical = { ...structuredClone(definition), id: "version-one", name: "Historical workflow", version: 1, isLatest: false, isPublished: true };
  historical.root.activities = [{ id: "historical", type: "Elsa.WriteLine", version: 1, name: "HistoricalOnly", metadata: { designer: { position: { x: 0, y: 0 } } } }];
  historical.root.connections = [];
  let versions = [historical, { ...definition, id: "version-two", version: 2, isLatest: false }, definition];
  const deleted: string[] = [];
  await page.route("**/elsa/api/**", async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    let json: unknown = { items: [], totalCount: 0 };
    if (path.endsWith("descriptors/activities")) json = { items: [{ typeName: "Elsa.WriteLine", name: "WriteLine", version: 1, displayName: "Write line", category: "Tests", kind: "Action", inputs: [], outputs: [], ports: [] }] };
    else if (path.endsWith("workflow-definitions/by-definition-id/parity")) json = url.searchParams.get("versionOptions") === "Latest" ? definition : versions.find(item => String(item.version) === url.searchParams.get("versionOptions"));
    else if (path.endsWith("workflow-definitions/parity/versions")) json = versions;
    else if (path.endsWith("workflow-definitions/parity/retract")) {
      definition = { ...definition, isPublished: false };
      json = definition;
    }
    else if (path.includes("workflow-definition-versions/") && request.method() === "DELETE") {
      const id = path.split("/").at(-1)!;
      deleted.push(id);
      versions = versions.filter(item => item.id !== id);
      await route.fulfill({ status: 204 });
      return;
    } else if (path.endsWith("workflow-definitions") && request.method() === "POST") {
      definition = { ...definition, ...request.postDataJSON().model };
      json = { workflowDefinition: definition };
    } else if (path.endsWith("workflow-definitions")) json = { items: [definition], totalCount: 1 };
    else if (path.endsWith("workflow-activation-strategies")) json = { items: [{ typeName: "ActivationStrategy", displayName: "Activation choice" }] };
    else if (path.endsWith("incident-strategies")) json = { items: [{ typeName: "IncidentStrategy", displayName: "Incident choice" }] };
    else if (path.endsWith("commit-strategies/workflows")) json = { items: [{ name: "CommitAlways", displayName: "Always commit" }] };
    else if (path.endsWith("descriptors/variables")) json = { items: [{ typeName: "String", displayName: "String" }] };
    else if (path.endsWith("storage-drivers")) json = { items: [{ typeName: "WorkflowStorage", displayName: "Workflow" }] };
    await route.fulfill({ json });
  });
  await page.goto("/studio-preview/workflows/definitions/parity/edit");
  await expect(page.locator(".react-flow").first()).toBeVisible();
  return { saved: () => definition, deleted };
}

function field(panel: Locator, label: string) { return panel.locator("label").filter({ hasText: new RegExp(`^${label}[*]?$`) }).locator(".."); }
async function choose(page: Page, panel: Locator, label: string, option: string) {
  await field(panel, label).getByRole("combobox").click();
  await page.getByRole("option", { name: option, exact: true }).click();
}

test("unpublishing consumes Core's bare definition response and updates the editor", async ({ page }) => {
  const fixture = await editorFixture(page, true);
  await expect(page.getByRole("button", { name: "Run", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "More", exact: true }).click();
  await page.getByRole("menuitem", { name: "Unpublish", exact: true }).click();
  await expect.poll(() => fixture.saved().isPublished).toBe(false);
  await expect(page.getByText("Workflow unpublished", { exact: true })).toBeVisible();
  await expect(page.getByText("Draft", { exact: true }).first()).toBeVisible();
  await expect(page.getByRole("button", { name: "Run", exact: true })).toHaveCount(0);
});

test("workflow settings, variable defaults and outcomes reach the normal save flow", async ({ page }) => {
  const fixture = await editorFixture(page);
  const panel = page.getByRole("tabpanel", { name: "Properties", exact: true });
  await field(panel, "Name").getByRole("textbox").fill("Audited workflow");
  await field(panel, "Description").getByRole("textbox").fill("Parity description");
  await choose(page, panel, "Activation strategy", "Activation choice");
  await choose(page, panel, "Incident strategy", "Incident choice");
  await choose(page, panel, "Commit strategy", "Always commit");
  await field(panel, "Usable as activity").getByRole("checkbox").check();
  await field(panel, "Auto-update referencing workflows").getByRole("checkbox").check();
  await field(panel, "Activity category").getByRole("textbox").fill("Parity category");
  await expect.poll(() => fixture.saved().options).toMatchObject({ activationStrategyType: "ActivationStrategy", incidentStrategyType: "IncidentStrategy", commitStrategyName: "CommitAlways", usableAsActivity: true, autoUpdateConsumingWorkflows: true, activityCategory: "Parity category" });
  await expect.poll(() => fixture.saved().name).toBe("Audited workflow");
  await expect.poll(() => fixture.saved().description).toBe("Parity description");
  await page.getByRole("tab", { name: "Variables", exact: true }).click();
  await page.getByRole("tabpanel", { name: "Variables", exact: true }).getByRole("button", { name: "Edit", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "Edit variable", exact: true });
  await field(dialog, "Default value").getByRole("textbox").fill("Updated default");
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect.poll(() => fixture.saved().variables[0].value).toBe("Updated default");
  await page.getByRole("tab", { name: "I/O", exact: true }).click();
  await page.getByPlaceholder("Outcome name").fill("Rejected");
  await page.getByPlaceholder("Outcome name").press("Enter");
  await expect.poll(() => fixture.saved().outcomes).toEqual(["Approved", "Rejected"]);
  await page.getByRole("tabpanel", { name: "I/O", exact: true }).getByText("Approved", { exact: true }).getByRole("button", { name: "Remove", exact: true }).click();
  await expect.poll(() => fixture.saved().outcomes).toEqual(["Rejected"]);
  await page.reload();
  await page.getByRole("tab", { name: "I/O", exact: true }).click();
  await expect(page.getByRole("tabpanel", { name: "I/O", exact: true }).getByText("Rejected", { exact: true })).toBeVisible();
});

test("version viewing is read-only and deletion targets the selected draft version", async ({ page }) => {
  const fixture = await editorFixture(page);
  await page.getByRole("tab", { name: "Versions", exact: true }).click();
  const versions = page.getByRole("tabpanel", { name: "Versions", exact: true });
  await versions.getByRole("row").filter({ hasText: "v1" }).getByRole("button", { name: "View", exact: true }).click();
  await expect(page).toHaveURL(/version=1/);
  await expect(page.locator(".react-flow").first()).toBeVisible();
  const name = field(page.getByRole("tabpanel", { name: "Properties", exact: true }), "Name").getByRole("textbox");
  await expect(name).toHaveAttribute("readonly", "");
  await expect(name).toHaveValue("Historical workflow");
  await expect(page.locator('.react-flow__node[data-id="historical"]')).toBeVisible();
  await expect(page.locator('.react-flow__node[data-id="alpha"]')).toHaveCount(0);
  await page.goto("/studio-preview/workflows/definitions/parity/edit");
  await expect(name).toHaveValue("Parity workflow");
  await expect(name).not.toHaveAttribute("readonly", "");
  await expect(page.locator('.react-flow__node[data-id="alpha"]')).toBeVisible();
  await expect(page.locator('.react-flow__node[data-id="historical"]')).toHaveCount(0);
  await page.getByRole("tab", { name: "Versions", exact: true }).click();
  await page.getByRole("tabpanel", { name: "Versions", exact: true }).getByRole("row").filter({ hasText: "v2" }).getByRole("button", { name: "Delete", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Delete", exact: true }).click();
  await expect.poll(() => fixture.deleted).toEqual(["version-two"]);
  await expect(page.getByRole("tabpanel", { name: "Versions", exact: true }).getByRole("row").filter({ hasText: "v2" })).toHaveCount(0);
});

test("canvas keyboard copy/paste, undo and redo update the saved graph", async ({ page }) => {
  const fixture = await editorFixture(page);
  await page.locator('.react-flow__node[data-id="alpha"]').click();
  await page.keyboard.press("Control+c");
  await page.keyboard.press("Control+v");
  await expect(page.locator(".react-flow__node")).toHaveCount(3);
  await page.keyboard.press("Control+z");
  await expect(page.locator(".react-flow__node")).toHaveCount(2);
  await page.keyboard.press("Control+Shift+z");
  await expect(page.locator(".react-flow__node")).toHaveCount(3);
  await expect.poll(() => (fixture.saved().root.activities as unknown[]).length).toBe(3);
  await page.reload();
  await expect(page.locator(".react-flow__node")).toHaveCount(3);
});
