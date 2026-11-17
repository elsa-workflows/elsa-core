import { expect, test, type Locator, type Page } from "@playwright/test";
import type { WorkflowDefinition } from "../packages/api-client/src/types/workflow-definitions";

async function editorFixture(page: Page, published = false, hasPublishedHistory = false) {
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
    } else if (path.endsWith("workflow-definitions/parity/dispatch")) json = { workflowInstanceId: "published-instance" };
    else if (path.endsWith("workflow-definitions")) {
      const items = url.searchParams.get("versionOptions") === "Published"
        ? definition.isPublished ? [definition] : hasPublishedHistory ? [historical] : []
        : [definition];
      json = { items, totalCount: items.length };
    }
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

async function contrast(element: Locator, foreground: string, background?: string) {
  return element.evaluate((target, { foreground, background }) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d")!;
    const style = getComputedStyle(target);
    const fill = (color: string) => {
      context.fillStyle = color.trim();
      context.fillRect(0, 0, 1, 1);
    };
    const luminance = () => {
      const [red, green, blue] = Array.from(context.getImageData(0, 0, 1, 1).data, value => {
        const channel = value / 255;
        return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
    };
    if (background) {
      fill(style.getPropertyValue(background));
    } else {
      const ancestors: Element[] = [];
      for (let element: Element | null = target; element; element = element.parentElement) ancestors.unshift(element);
      for (const element of ancestors) fill(getComputedStyle(element).backgroundColor);
    }
    const adjacent = luminance();
    fill(style.getPropertyValue(foreground));
    const indicator = luminance();
    return (Math.max(adjacent, indicator) + 0.05) / (Math.min(adjacent, indicator) + 0.05);
  }, { foreground, background });
}

test("Classic light workflow controls and selected activity have sufficient contrast", async ({ page }) => {
  await page.emulateMedia({ colorScheme: "light" });
  await editorFixture(page, true);
  await expect(page.locator("html")).toHaveAttribute("data-elsa-theme", "classic");

  const create = page.getByRole("button", { name: "New workflow", exact: true });
  await expect(create).toBeVisible();
  expect(await contrast(create, "color")).toBeGreaterThanOrEqual(4.5);
  await create.hover();
  expect(await contrast(create, "color")).toBeGreaterThanOrEqual(4.5);
  await page.keyboard.press("Tab");
  await create.focus();
  await expect(create).toBeFocused();
  expect(await contrast(create, "--tw-ring-color", "--sidebar")).toBeGreaterThanOrEqual(3);
  const publishedBadge = page.getByRole("navigation", { name: "Breadcrumb" }).getByText("Published", { exact: true });
  expect(await contrast(publishedBadge, "color")).toBeGreaterThanOrEqual(4.5);

  const activity = page.locator('.react-flow__node[data-id="alpha"]');
  await activity.click();
  const selectedCard = activity.locator(".accent-rail");
  await expect(selectedCard).toHaveClass(/\bring-primary\b/);
  expect(await contrast(selectedCard, "--tw-ring-color", "--elsa-canvas")).toBeGreaterThanOrEqual(3);

  await page.goto("/studio-preview/workflows/definitions");
  const column = page.getByRole("columnheader", { name: "Name", exact: true });
  await expect(column).toBeVisible();
  expect(await contrast(column, "color")).toBeGreaterThanOrEqual(4.5);
});

test("a newer draft can run the workflow's published version", async ({ page }) => {
  await editorFixture(page, false, true);
  await expect(page.getByRole("navigation", { name: "Breadcrumb" }).getByText("Draft")).toBeVisible();
  await page.getByRole("button", { name: "Run", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "Run Historical workflow" });
  const dispatch = page.waitForRequest(request =>
    request.method() === "POST" && request.url().endsWith("/workflow-definitions/parity/dispatch"));
  await dialog.getByRole("button", { name: "Run", exact: true }).click();
  await dispatch;
  await expect(page.getByText("Workflow started", { exact: true })).toBeVisible();
});

test("workflow forms and lists expose named controls and one main landmark", async ({ page }) => {
  await editorFixture(page);
  await expect(page.getByRole("main")).toHaveCount(1);
  const invalidMenuChildren = page.locator('[data-slot="sidebar-menu"] > :not(li), [data-slot="sidebar-menu-sub"] > :not(li)');
  await expect(invalidMenuChildren).toHaveCount(0);
  const properties = page.getByRole("tabpanel", { name: "Properties", exact: true });
  await expect(properties.getByRole("textbox", { name: "Name *", exact: true })).toHaveValue("Parity workflow");
  await expect(properties.getByRole("textbox", { name: "Description", exact: true })).toBeVisible();
  for (const name of ["Activation strategy", "Incident strategy", "Log persistence strategy", "Commit strategy"]) {
    await expect(properties.getByRole("combobox", { name, exact: true })).toBeVisible();
  }
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByRole("columnheader", { name: "Actions", exact: true })).toBeVisible();
  await expect(page.getByRole("checkbox", { name: "Select Parity workflow", exact: true })).toBeVisible();
  await expect(page.getByRole("combobox", { name: "Rows per page", exact: true })).toBeVisible();
  await page.goto("/studio-preview/workflows/instances");
  await expect(page.getByRole("combobox", { name: "Incidents", exact: true })).toBeVisible();
});

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
