import { expect, test } from "@playwright/test";

for (const container of [false, true]) {
test(`opening a ${container ? "populated Flowchart" : "single-activity workflow"} preserves its root until a graph edit`, async ({ page }) => {
  const activity = { id: "single-root", type: "Elsa.WriteLine", version: 1,
    text: { typeName: "String", expression: { type: "Literal", value: "Keep this activity" } },
    metadata: { designer: { position: { x: 12, y: 34 }, pluginData: "keep" }, custom: "keep" },
    customProperties: { plugin: { keep: true } } };
  const originalRoot = container ? { id: "flowchart-root", type: "Elsa.Flowchart", version: 1, activities: [activity], connections: [],
    customProperties: { plugin: { id: "user-data", type: "UserPayload", metadata: { designer: { size: { width: 123, height: 456 } } } } } } : activity;
  let definition = { id: "single-version", definitionId: "single", name: "Single activity", version: 1, isLatest: true, isPublished: false,
    createdAt: "2026-10-21T00:00:00Z", variables: [], inputs: [], outputs: [], outcomes: [], options: {}, customProperties: {}, root: originalRoot };
  const saves: Record<string, unknown>[] = [];
  await page.route("**/elsa/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    let json: unknown = { items: [], totalCount: 0 };
    if (path.endsWith("workflow-definitions/by-definition-id/single")) json = definition;
    else if (path.endsWith("descriptors/activities")) json = { items: [{ typeName: "Elsa.WriteLine", name: "WriteLine", displayName: "Write line", version: 1, kind: "Action", category: "Tests", ports: [], inputs: [], outputs: [] }] };
    else if (path.endsWith("workflow-definitions") && route.request().method() === "POST") {
      const model = route.request().postDataJSON().model;
      saves.push(model);
      definition = { ...definition, ...model };
      json = { workflowDefinition: definition };
    }
    await route.fulfill({ json });
  });
  await page.goto("/studio-preview/workflows/definitions/single/edit");
  await expect(page.locator('.react-flow__node[data-id="single-root"]')).toBeVisible();
  // The production autosave delay is two seconds; observe beyond it.
  await page.waitForTimeout(3000);
  expect(saves).toEqual([]);
  const properties = page.getByRole("tabpanel", { name: "Properties", exact: true });
  await properties.locator("label").filter({ hasText: /^Name[*]?$/ }).locator("..").getByRole("textbox").fill("Metadata edit");
  await expect.poll(() => saves.length).toBe(1);
  expect(saves[0].root).toEqual(originalRoot);
  expect(saves[0].name).toBe("Metadata edit");
});

}
