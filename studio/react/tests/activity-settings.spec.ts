import { expect, test } from "@playwright/test";

const descriptor = { typeName: "Example.Reusable", name: "Reusable", displayName: "Reusable task", version: 1, category: "Tests", kind: "Task",
  inputs: [{ name: "Message", typeName: "String" }], outputs: [{ name: "Result", typeName: "String" }], ports: [], customProperties: { RootType: "WorkflowDefinitionActivity", Resilient: true } };
const activity = { id: "child", nodeId: "root:child", type: descriptor.typeName, version: 1, workflowDefinitionId: "reusable", workflowDefinitionVersionId: "version-one",
  message: { typeName: "String", expression: { type: "Literal", value: "keep" } }, customProperties: { plugin: "keep", logPersistenceMode: { inputs: { message: "Include" } } } };

for (const catalogFailure of [false, true]) {
test(catalogFailure ? "catalog failures preserve local clear and expression actions" : "activity settings edit current server fields, preserve adjacent data, select a version, and honor read-only", async ({ page }) => {
  const fixtureActivity = catalogFailure ? { ...activity, customProperties: { ...activity.customProperties, commitStrategyName: "Always", resilienceStrategy: { mode: "Identifier", strategyId: "retry-three" } } } : activity;
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.route("**/elsa/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (catalogFailure && ["commit-strategies/activities", "log-persistence-strategies", "resilience/strategies"].some(suffix => path.endsWith(suffix))) {
      await route.fulfill({ status: 503, json: { title: "Catalog unavailable" } });
      return;
    }
    const items = path.endsWith("features/installed") ? [{ name: "Resilience", namespace: "Elsa", fullName: "Elsa.Resilience", displayName: "Resilience" }]
      : path.endsWith("commit-strategies/activities") ? [{ name: "Always", displayName: "Always commit" }]
      : path.endsWith("log-persistence-strategies") ? [{ typeName: "Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core", displayName: "Include" }, { typeName: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core", displayName: "Exclude" }]
      : path.endsWith("resilience/strategies") ? [{ id: "retry-three", displayName: "Retry three times" }]
      : path.endsWith("workflow-definitions") ? [{ id: "version-two", definitionId: "reusable", version: 2, isPublished: true }, { id: "version-one", definitionId: "reusable", version: 1 }, { id: "version-unavailable", definitionId: "reusable", version: 3 }]
      : [];
    if (path.endsWith("workflow-definitions")) expect(new URL(route.request().url()).searchParams.get("versionOptions")).toBe("AllVersions");
    await route.fulfill({ json: { items, totalCount: items.length } });
  });
  let originalEntry = "";
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch();
    const manifest = await response.json();
    const editors = manifest.modules.find((module: { id: string }) => module.id === "activity-editors");
    originalEntry = editors.entry;
    editors.entry = "/settings-fixture.js";
    await route.fulfill({ json: manifest });
  });
  await page.route("**/settings-fixture.js", route => route.fulfill({ contentType: "text/javascript", body: `
    import { createElement as h, useState } from "react";
    import { useStore } from "zustand";
    import { ActivityPropertiesPanel } from "@elsa-workflows/react-studio-sdk/components";
    import { ActivityRegistry, createDesignerStore, DesignerProvider } from "@elsa-workflows/react-studio-sdk/designer/index";
    import { register as registerEditors } from ${JSON.stringify(originalEntry)};
    const child = ${JSON.stringify(fixtureActivity)};
    const registry = new ActivityRegistry([${JSON.stringify(descriptor)}, {...${JSON.stringify(descriptor)},version:2}]);
    function Fixture() {
      const [store] = useState(()=>{const value=createDesignerStore({id:"root",type:"Elsa.Flowchart",version:1,activities:[child],connections:[]});value.getState().select(["child"]);return value;});
      const root = useStore(store,state=>state.root);
      return h(DesignerProvider,{store,registry},h("section",{style:{height:"850px",width:"700px"}},
        h("h1",null,"Activity settings fixture"),
        h("button",{onClick:()=>store.setState(state=>({readOnly:!state.readOnly}))},"Toggle read only"),
        h("output",{"data-testid":"saved-activity"},JSON.stringify(root.activities[0])),
        h(ActivityPropertiesPanel,{variables:[],workflowInputs:[],workflowOutputs:[]})));
    }
    export function register(api) {registerEditors(api);api.contribute("routes",{id:"settings-fixture",path:"/settings-fixture",component:Fixture});}
  ` }));
  await page.goto("/studio-preview/settings-fixture");
  await expect(page.getByRole("heading", { name: "Activity settings fixture" })).toBeVisible();
  const saved = page.getByTestId("saved-activity");
  await expect(saved).toHaveText(JSON.stringify(fixtureActivity));
  if (catalogFailure) {
    await page.getByRole("tab", { name: "Commit strategy", exact: true }).click();
    await expect(page.getByRole("alert").filter({ hasText: "Could not load commit strategies" })).toBeVisible();
    await page.getByRole("combobox", { name: "Commit strategy", exact: true }).click();
    await page.getByRole("option", { name: "Inherit", exact: true }).click();
    await expect(saved).not.toContainText('"commitStrategyName"');
    await page.getByRole("tab", { name: "Log", exact: true }).click();
    await expect(page.getByRole("alert").filter({ hasText: "Could not load log persistence strategies" })).toBeVisible();
    await page.getByLabel("Input: Message mode").click();
    await page.getByRole("option", { name: "Expression", exact: true }).click();
    await page.getByLabel("Input: Message expression value").click();
    await page.getByRole("option", { name: "Include", exact: true }).click();
    await expect(saved).toContainText('"expression":{"type":"Literal","value":"Include"}');
    await page.getByLabel("Input: Message mode").click();
    await page.getByRole("option", { name: "Inherit", exact: true }).click();
    await expect(saved).toContainText('"message":null');
    await page.getByRole("tab", { name: "Resilience", exact: true }).click();
    await expect(page.getByRole("alert").filter({ hasText: "Could not load resilience strategies" })).toBeVisible();
    await page.getByLabel("Resilience mode").click();
    await page.getByRole("option", { name: "Expression", exact: true }).click();
    await page.getByLabel("Resilience expression value").fill("fallback-policy");
    await expect(saved).toContainText('"expression":{"type":"Literal","value":"fallback-policy"}');
    await page.getByLabel("Resilience mode").click();
    await page.getByRole("option", { name: "Default", exact: true }).click();
    await expect(saved).not.toContainText('"resilienceStrategy"');
    await page.getByRole("button", { name: "Toggle read only", exact: true }).click();
    await expect(page.getByLabel("Resilience mode")).toBeDisabled();
    expect(errors).toEqual([]);
    return;
  }
  await page.getByRole("tab", { name: "Commit strategy", exact: true }).click();
  await page.getByRole("combobox", { name: "Commit strategy", exact: true }).click();
  await page.getByRole("option", { name: "Always commit", exact: true }).click();
  await expect(saved).toContainText('"commitStrategyName":"Always"');
  await page.getByRole("tab", { name: "Log", exact: true }).click();
  await expect(page.getByLabel("Input: Message strategy")).toContainText("Include");
  await page.getByLabel("Input: Message mode").click();
  await page.getByRole("option", { name: "Expression", exact: true }).click();
  await page.getByLabel("Input: Message expression value").click();
  await page.getByRole("option", { name: "Include", exact: true }).click();
  await expect(saved).toContainText('"expression":{"type":"Literal","value":"Include"}');
  await page.getByLabel("Input: Message mode").click();
  await page.getByRole("option", { name: "Inherit", exact: true }).click();
  const cleared = JSON.parse((await saved.textContent())!);
  expect(cleared.customProperties.logPersistenceConfig.inputs.message).toBeNull();
  expect(cleared.customProperties.logPersistenceMode.inputs.message).toBeUndefined();
  await page.getByLabel("Output: Result mode").click();
  await page.getByRole("option", { name: "Strategy", exact: true }).click();
  await page.getByLabel("Output: Result strategy").click();
  await page.getByRole("option", { name: "Exclude", exact: true }).click();
  await expect(saved).toContainText('"strategyType":"Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core"');
  await page.getByRole("tab", { name: "Task", exact: true }).click();
  await page.getByLabel("Run asynchronously", { exact: true }).check();
  await expect(saved).toContainText('"runAsynchronously":true');
  await page.getByRole("tab", { name: "Resilience", exact: true }).click();
  await page.getByLabel("Resilience mode").click();
  await page.getByRole("option", { name: "Strategy", exact: true }).click();
  await page.getByLabel("Resilience strategy", { exact: true }).click();
  await page.getByRole("option", { name: "Retry three times", exact: true }).click();
  await expect(saved).toContainText('"resilienceStrategy":{"mode":"Identifier","strategyId":"retry-three"}');
  await page.getByRole("tab", { name: "Version", exact: true }).click();
  await expect(page.getByRole("button", { name: "Use version 3", exact: true })).toBeDisabled();
  await page.getByRole("button", { name: "Use version 2", exact: true }).click();
  await expect(saved).toContainText('"version":2');
  await expect(saved).toContainText('"workflowDefinitionVersionId":"version-two"');
  await expect(saved).toContainText('"plugin":"keep"');
  await expect(saved).toContainText('"message":{"typeName":"String","expression":{"type":"Literal","value":"keep"}}');
  await page.getByRole("button", { name: "Toggle read only", exact: true }).click();
  await expect(page.getByRole("button", { name: "Use version 1", exact: true })).toBeDisabled();
  await page.getByRole("tab", { name: "Task", exact: true }).click();
  await expect(page.getByLabel("Run asynchronously", { exact: true })).toBeDisabled();
  await page.getByRole("tab", { name: "Commit strategy", exact: true }).click();
  await expect(page.getByRole("combobox", { name: "Commit strategy", exact: true })).toBeDisabled();
  expect(errors).toEqual([]);
});

}

for (const failNextPage of [false, true]) test(`instance retries select executions and recover paging (failed page: ${failNextPage})`, async ({ page }) => {
  const errors: string[] = [];
  const calls: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  const executions = ["first", "latest"].map((name, index) => ({ id: `execution-${name}`, workflowInstanceId: "instance", activityId: "child", activityNodeId: "root:child", activityType: descriptor.typeName,
    activityTypeVersion: 1, activityName: "Retry task", status: "Completed", startedAt: `2026-10-20T10:00:0${index}.000Z`, completedAt: `2026-10-20T10:00:0${index + 1}.000Z` }));
  await page.route("**/elsa/api/**", async route => {
    const url = new URL(route.request().url());
    const path = url.pathname;
    let json: unknown = { items: [], totalCount: 0 };
    if (path.endsWith("descriptors/activities")) json = { items: [descriptor] };
    else if (path.endsWith("workflow-instances/instance")) json = { id: "instance", definitionId: "parent", definitionVersionId: "parent-version", version: 1, status: "Finished", subStatus: "Finished", workflowState: { incidents: [], bookmarks: [] } };
    else if (path.endsWith("workflow-definitions/by-id/parent-version")) json = { id: "parent-version", definitionId: "parent", version: 1, name: "Retry example", root: { id: "root", type: "Elsa.Flowchart", version: 1, activities: [activity], connections: [] } };
    else if (path.endsWith("activity-executions/list")) json = { items: executions };
    else if (path.endsWith("activity-executions/report")) json = { stats: [] };
    else if (path.includes("resilience/retries/")) {
      calls.push(`${path}?${url.searchParams}`);
      const latest = path.endsWith("execution-latest");
      const skip = Number(url.searchParams.get("skip"));
      if (failNextPage && skip > 0) { await route.fulfill({ status: 503, json: { message: "Retry page unavailable" } }); return; }
      json = { items: latest ? Array.from({ length: skip === 0 ? 10 : 1 }, (_, index) => ({ id: `attempt-${skip + index}`, activityInstanceId: "execution-latest", attemptNumber: skip + index,
        retryDelay: "00:00:01", details: { exception: "Temporary failure", custom: "Preserved details" } })) : [], totalCount: latest ? 11 : 0 };
    }
    await route.fulfill({ json });
  });
  await page.goto("/studio-preview/workflows/instances/instance/view");
  await page.locator('.react-flow__node[data-id="child"]').click();
  await page.getByRole("tab", { name: "Retries", exact: true }).click();
  await expect(page.getByText("11 attempts", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Details for attempt 0", exact: true }).click();
  await expect(page.getByText("Preserved details", { exact: false })).toBeVisible();
  await page.getByRole("button", { name: "Next attempts", exact: true }).click();
  if (failNextPage) {
    await expect(page.getByText("Could not load retry attempts", { exact: true })).toBeVisible({ timeout: 20_000 });
    await expect(page.getByRole("button", { name: "Next attempts", exact: true })).toBeDisabled();
    await page.getByRole("button", { name: "Previous attempts", exact: true }).click();
    await expect(page.getByRole("button", { name: "Details for attempt 0", exact: true })).toBeVisible();
  } else {
    await expect(page.getByRole("button", { name: "Details for attempt 10", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Next attempts", exact: true })).toBeDisabled();
  }
  await page.getByLabel("Retry activity execution").click();
  await page.getByRole("option", { name: /execution-first/ }).click();
  await expect(page.getByText("No retry attempts for this execution.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Details for attempt 10", exact: true })).toHaveCount(0);
  expect(calls.some(call => call.includes("execution-latest?skip=10&take=10"))).toBe(true);
  expect(calls.some(call => call.includes("execution-first?skip=0&take=10"))).toBe(true);
  expect(calls.every(call => !call.includes("retries/child"))).toBe(true);
  expect(errors).toEqual([]);
});
