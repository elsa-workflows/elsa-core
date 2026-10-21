import { readFile } from "node:fs/promises";
import { expect, test, type Page, type Route } from "@playwright/test";

const xml = '<definitions xmlns="http://www.omg.org/spec/BPMN/20100524/MODEL"><process id="first"/></definitions>';
const definition = { id: "version", definitionId: "imported", name: "Imported workflow", version: 1, isLatest: true, isPublished: false, createdAt: "2026-10-21T00:00:00Z", variables: [], inputs: [], outputs: [], outcomes: [], options: {}, customProperties: {}, root: { id: "root", type: "Elsa.BpmnProcess", version: 1, definition: { processId: "first", elements: [] }, activities: [] } };

async function fixture(page: Page, refusal?: { operation: "import" | "export"; code: string; data?: object }, findings = false) {
  const uploads: string[] = [];
  const writes: unknown[] = [];
  await page.route("**/elsa/api/**", async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path.endsWith("bpmn/analyze")) {
      await route.fulfill({ json: { processIds: findings ? ["first", "second"] : ["first"], elementCounts: { process: 1 }, issues: findings ? [
        { severity: "Info", elementId: "Event1", message: "Information retained" },
        { severity: "Degraded", elementId: "Task1", message: "Behavior degraded" },
        { severity: "Dropped", elementId: "Lane1", message: "Lane was dropped" },
      ] : [] } }); return;
    }
    if (path.endsWith("bpmn/import") || path.endsWith("/export")) {
      const operation = path.endsWith("bpmn/import") ? "import" : "export";
      if (operation === "import") uploads.push(request.postDataBuffer()!.toString());
      if (refusal?.operation === operation) { await route.fulfill({ status: 422, json: { message: "Server-specific explanation", code: refusal.code, data: refusal.data } }); return; }
      await route.fulfill(operation === "export" ? { contentType: "application/xml", body: xml } : { json: { id: definition.id, definitionId: definition.definitionId, version: 1, analysis: {} } }); return;
    }
    let json: unknown = { items: [], totalCount: 0 };
    if (path.endsWith("workflow-definitions") && request.method() === "POST") writes.push(request.postDataJSON());
    if (path.endsWith("workflow-definitions")) json = { items: [definition], totalCount: 1 };
    if (path.includes("workflow-definitions/by-definition-id/")) json = definition;
    await route.fulfill({ json });
  });
  await page.goto("/studio-preview/workflows/definitions");
  await expect(page.getByRole("link", { name: "Imported workflow", exact: true })).toBeVisible();
  return { uploads, writes };
}

async function openImport(page: Page, content = xml) {
  await page.getByRole("button", { name: "Import BPMN", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "Import BPMN", exact: true });
  await dialog.getByLabel("BPMN document (up to 10 MB)").setInputFiles({ name: "sample.bpmn", mimeType: "application/xml", buffer: Buffer.from(content) });
  return dialog;
}

test("findings require process selection, cancellation persists nothing, and import uses the selected original file", async ({ page }) => {
  const { uploads } = await fixture(page, undefined, true);
  let dialog = await openImport(page);
  for (const severity of ["Info", "Degraded", "Dropped"]) await expect(dialog.getByRole("heading", { name: severity, exact: true })).toBeVisible();
  await expect(dialog.getByText("Lane was dropped", { exact: false })).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Import", exact: true })).toBeDisabled();
  await dialog.getByRole("button", { name: "Cancel", exact: true }).click();
  expect(uploads).toEqual([]);
  dialog = await openImport(page);
  await dialog.getByLabel("Process", { exact: true }).selectOption("second");
  await dialog.getByRole("button", { name: "Import", exact: true }).click();
  await expect(page).toHaveURL(/definitions\/imported\/edit$/);
  expect(uploads).toHaveLength(1);
  expect(uploads[0]).toContain(xml);
  expect(uploads[0]).toMatch(/name="ProcessId"\r\n\r\nsecond/);
});

for (const refusal of [
  { operation: "import" as const, code: "bpmn.import.capability-unsupported", data: { capabilities: ["ServiceTasks"], elementIds: ["Task42"] }, text: "ServiceTasks" },
  { operation: "import" as const, code: "bpmn.import.condition-unsupported", data: { elementIds: ["Flow42"] }, text: "Flow42" },
  { operation: "export" as const, code: "bpmn.export.source-stale", text: "The workflow changed after import." },
  { operation: "export" as const, code: "bpmn.export.not-imported", text: "This workflow has no recorded BPMN source to export." },
  { operation: "export" as const, code: "future.unknown", text: "Server-specific explanation" },
]) test(`BPMN refusal preserves structured and server messages: ${refusal.code}`, async ({ page }) => {
  await fixture(page, refusal);
  let dialog;
  if (refusal.operation === "import") {
    dialog = await openImport(page);
    await expect(dialog.getByText("No findings. This document reads without loss.")).toBeVisible();
    await dialog.getByRole("button", { name: "Import", exact: true }).click();
  } else {
    await page.getByRole("button", { name: "Actions", exact: true }).click();
    await page.getByRole("menuitem", { name: "Export BPMN", exact: true }).click();
    dialog = page.getByRole("dialog", { name: "Export BPMN", exact: true });
    await dialog.getByRole("button", { name: "Export", exact: true }).click();
  }
  await expect(dialog.getByText(refusal.text, { exact: false }).first()).toBeVisible();
  await expect(dialog.getByText("Server-specific explanation", { exact: false }).first()).toBeVisible();
  await expect(dialog).toBeVisible();
});

test("BPMN download retains XML bytes and can be imported again", async ({ page }) => {
  const { uploads } = await fixture(page);
  await page.getByRole("button", { name: "Actions", exact: true }).click();
  await page.getByRole("menuitem", { name: "Export BPMN", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "Export BPMN", exact: true });
  await expect(dialog.getByText(/binding configuration and expressions/)).toBeVisible();
  const downloaded = page.waitForEvent("download");
  await dialog.getByRole("button", { name: "Export", exact: true }).click();
  const download = await downloaded;
  expect(download.suggestedFilename()).toBe("imported.bpmn");
  const exported = await readFile((await download.path())!, "utf8");
  expect(exported).toBe(xml);
  const importing = await openImport(page, exported);
  await expect(importing.getByRole("button", { name: "Import", exact: true })).toBeEnabled();
  await importing.getByRole("button", { name: "Import", exact: true }).click();
  await expect(page).toHaveURL(/definitions\/imported\/edit$/);
  expect(uploads[0]).toContain(xml);
});

test("an imported BPMN root stays read-only without normalization or autosave and still exports", async ({ page }) => {
  await page.clock.install();
  const { writes } = await fixture(page);
  const importing = await openImport(page);
  await expect(importing.getByRole("button", { name: "Import", exact: true })).toBeEnabled();
  await importing.getByRole("button", { name: "Import", exact: true }).click();
  await expect(page.getByText("Read-only BPMN definition", { exact: true })).toBeVisible();
  await expect(page.locator(".react-flow")).toHaveCount(0);
  await page.clock.fastForward(10_000);
  expect(writes).toEqual([]);
  await page.getByRole("button", { name: "Export BPMN", exact: true }).click();
  const downloaded = page.waitForEvent("download");
  await page.getByRole("dialog", { name: "Export BPMN", exact: true }).getByRole("button", { name: "Export", exact: true }).click();
  expect((await downloaded).suggestedFilename()).toBe("imported.bpmn");
  expect(writes).toEqual([]);
});

for (const operation of ["analysis", "export"] as const) test(`cancel aborts pending BPMN ${operation} without later navigation or download`, async ({ page }) => {
  const { uploads, writes } = await fixture(page);
  let pending!: Route;
  let started!: () => void;
  const intercepted = new Promise<void>(resolve => { started = resolve; });
  const path = operation === "analysis" ? "**/bpmn/analyze" : "**/bpmn/definitions/imported/export";
  await page.route(path, route => { pending = route; started(); });
  let downloads = 0;
  page.on("download", () => downloads++);
  const aborted = page.waitForEvent("requestfailed", request => request.url().endsWith(operation === "analysis" ? "/bpmn/analyze" : "/export"));
  let dialog;
  if (operation === "analysis") dialog = await openImport(page);
  else {
    await page.getByRole("button", { name: "Actions", exact: true }).click();
    await page.getByRole("menuitem", { name: "Export BPMN", exact: true }).click();
    dialog = page.getByRole("dialog", { name: "Export BPMN", exact: true });
    await dialog.getByRole("button", { name: "Export", exact: true }).click();
  }
  await intercepted;
  await dialog.getByRole("button", { name: "Cancel", exact: true }).click();
  expect((await aborted).failure()?.errorText).toContain("ERR_ABORTED");
  // An aborted browser request may reject fulfillment; either way it cannot
  // deliver a late result to the disposed component.
  await pending.fulfill(operation === "analysis" ? { json: { processIds: ["first"], issues: [], elementCounts: {} } } : { contentType: "application/xml", body: xml }).catch(() => {});
  await expect(dialog).toHaveCount(0);
  await expect(page).toHaveURL(/workflows\/definitions$/);
  expect(uploads).toEqual([]);
  expect(writes).toEqual([]);
  expect(downloads).toBe(0);
});
