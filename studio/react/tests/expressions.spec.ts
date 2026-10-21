import { expect, test, type Page } from "@playwright/test";

async function fixture(page: Page, unavailable = false) {
  let calls = 0;
  await page.route("**/elsa/api/**", async route => {
    if (route.request().url().includes("scripting/javascript/type-definitions/fixture")) {
      calls++;
      expect(route.request().postDataJSON()).toEqual({ activityTypeName: "Example.Activity", propertyName: "Value" });
      await route.fulfill(unavailable ? { status: 503, json: { message: "Unavailable" } } : { contentType: "application/x-typescript", body: "declare class Customer {\nName: string;\n}\ndeclare function lookupServerValue(): string;\ndeclare function getCustomerId(): Customer;\ndeclare function setCustomer(value: Customer): void;" });
    } else await route.fulfill({ json: { items: [], totalCount: 0 } });
  });
  let originalEntry = "";
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch();
    const manifest = await response.json();
    const editors = manifest.modules.find((module: { id: string }) => module.id === "activity-editors");
    originalEntry = editors.entry;
    editors.entry = "/expression-fixture.js";
    await route.fulfill({ json: manifest });
  });
  await page.route("**/expression-fixture.js", route => route.fulfill({ contentType: "text/javascript", body: `
    import { createElement as h, useState } from "react";
    import { useExtensions } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
    import { register as registerEditors } from ${JSON.stringify(originalEntry)};
    const context = {workflowDefinitionId:"fixture",activityType:"Example.Activity",activityVersion:1,activity:{id:"activity"},variables:[{id:"customer",name:"customer",typeName:"Customer"}],outputs:[],inputs:[{name:"Customer",type:"Customer"},{name:"customer_id",type:"Customer"}],activities:[{name:"Lookup",outputs:["Result"]}]};
    function Fixture() {
      const hints = useExtensions("uiHints");
      const [value,setValue] = useState({typeName:"String",expression:{type:"CSharp",value:"var count = 1;"}});
      const [readOnly,setReadOnly] = useState(false);
      return h("section",null,h("h1",null,"Expression fixture"),
        h("button",{onClick:()=>setReadOnly(current=>!current)},"Toggle read only"),
        ...["CSharp","JavaScript","Python"].map(type=>h("button",{key:type,onClick:()=>setValue({typeName:"String",expression:{type,value:""}})},type)),
        h(hints.singleline,{descriptor:{name:"Value",typeName:"String",defaultSyntax:"Literal"},value,onChange:setValue,context,readOnly,syntaxes:["Literal","CSharp","JavaScript","Python"]}),
        h("output",{"data-testid":"value"},value.expression.value));
    }
    export function register(api) { registerEditors(api); api.contribute("routes",{id:"expression-fixture",path:"/expression-fixture",component:Fixture}); }
  ` }));
  await page.goto("/studio-preview/expression-fixture");
  await expect(page.getByRole("heading", { name: "Expression fixture" })).toBeVisible();
  return () => calls;
}

async function complete(page: Page, prefix: string, label: string, expected = label, beforeClosingParenthesis = false) {
  const editor = page.locator(".cm-content");
  const cursor = prefix.indexOf("|");
  await editor.fill(prefix.replace("|", ""));
  if (cursor >= 0) for (let i = 0; i < prefix.length - cursor - 1; i++) await editor.press("ArrowLeft");
  if (beforeClosingParenthesis) await editor.press("ArrowLeft");
  await editor.press("Control+Space");
  const option = page.getByRole("option").filter({ has: page.getByText(label, { exact: true }) });
  await expect(option).toBeVisible();
  await option.click();
  await expect(page.getByTestId("value")).toHaveText(expected);
}

test("C# highlights syntax and all three languages complete server members, inputs and outputs without refetching on edits", async ({ page }) => {
  const calls = await fixture(page);
  await expect(page.locator(".cm-line span").filter({ hasText: /^var$/ })).toBeVisible();
  await complete(page, "Variable.Customer.", "Variable.Customer.Name");
  await complete(page, "Input.Customer.", "Input.Customer.Name");
  await complete(page, "someFunction(Variable.C", "Variable.Customer", "someFunction(Variable.Customer");
  await complete(page, "[Variable.C", "Variable.Customer", "[Variable.Customer");
  await complete(page, "Out", 'Output.From("Lookup", "Result")');
  await complete(page, 'Output.From("Lookup", "Res', 'Output.From("Lookup", "Result")');
  await complete(page, 'Math.Max(Output.From("Lookup", "Res', 'Output.From("Lookup", "Result")', 'Math.Max(Output.From("Lookup", "Result")');
  await page.getByRole("button", { name: "JavaScript", exact: true }).click();
  await complete(page, "variables.customer.", "variables.customer.Name");
  await complete(page, "getIn", 'getInput("Customer")');
  await complete(page, "getOut", 'getOutputFrom("Lookup", "Result")');
  await complete(page, 'getOutputFrom("Lookup", "Res', 'getOutputFrom("Lookup", "Result")');
  await complete(page, 'Math.max(getOutputFrom("Lookup", "Res', 'getOutputFrom("Lookup", "Result")', 'Math.max(getOutputFrom("Lookup", "Result")');
  await complete(page, "someFunction(getcustomer_id().N)", "getcustomer_id().Name", "someFunction(getcustomer_id().Name)", true);
  await complete(page, "lookupServer", "lookupServerValue");
  await page.getByRole("button", { name: "Python", exact: true }).click();
  await complete(page, "variables.customer.", "variables.customer.Name");
  await complete(page, "input.G", 'input.Get("Customer")');
  await complete(page, "output.G", 'output.Get("Lookup", "Result")');
  expect(calls()).toBe(1);
  await page.getByRole("button", { name: "Toggle read only", exact: true }).click();
  await expect(page.locator(".cm-content")).toHaveAttribute("contenteditable", "false");
});

test("quoted accessor completion replaces its existing suffix and preserves enclosing expressions", async ({ page }) => {
  await fixture(page);
  await complete(page, 'Math.Max(Output.From("Lookup", "Res|"), 1)', 'Output.From("Lookup", "Result")', 'Math.Max(Output.From("Lookup", "Result"), 1)');
  await page.getByRole("button", { name: "JavaScript", exact: true }).click();
  await complete(page, 'getInput("Cus|")', 'getInput("Customer")');
  await complete(page, 'Math.max(getOutputFrom("Lookup", "Res|"), 1)', 'getOutputFrom("Lookup", "Result")', 'Math.max(getOutputFrom("Lookup", "Result"), 1)');
  await page.getByRole("button", { name: "Python", exact: true }).click();
  await complete(page, 'input.Get("Cus|")', 'input.Get("Customer")');
});

test("setter value arguments retain ordinary variable completion", async ({ page }) => {
  await fixture(page);
  await page.getByRole("button", { name: "JavaScript", exact: true }).click();
  await complete(page, "setCustomer(variables.c", "variables.customer", "setCustomer(variables.customer");
  await complete(page, 'getOutputFrom("Lookup", variables.c', "variables.customer", 'getOutputFrom("Lookup", variables.customer');
});

test("server type failure leaves local workflow member completion usable", async ({ page }) => {
  await fixture(page, true);
  await expect(page.getByRole("status").filter({ hasText: "Server type information is unavailable" })).toBeVisible();
  await complete(page, "Variable.C", "Variable.Customer");
});

test("a pending server type request does not keep a successful workflow save pending", async ({ page }) => {
  let release!: () => void;
  const paused = new Promise<void>(resolve => { release = resolve; });
  let pending: Promise<void> | undefined;
  let saved = false;
  let definition = { id: "version", definitionId: "slow-types", version: 1, isLatest: true, isPublished: false, name: "Slow types", createdAt: "2026-10-21T00:00:00Z",
    variables: [], inputs: [], outputs: [], outcomes: [], options: {}, customProperties: {}, root: { id: "root", type: "Elsa.Flowchart", version: 1, connections: [], activities: [
      { id: "child", type: "Elsa.WriteLine", version: 1, text: { typeName: "String", expression: { type: "CSharp", value: "1" } } },
    ] } };
  await page.route("**/elsa/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.includes("scripting/javascript/type-definitions/")) {
      pending = paused.then(() => route.fulfill({ contentType: "application/x-typescript", body: "" }));
      await pending;
      return;
    }
    let json: unknown = { items: [], totalCount: 0 };
    if (path.endsWith("descriptors/activities")) json = { items: [{ typeName: "Elsa.WriteLine", name: "WriteLine", displayName: "Write line", version: 1, kind: "Action", category: "Tests", ports: [], outputs: [], inputs: [{ name: "Text", typeName: "String", uiHint: "singleline", isWrapped: true, defaultSyntax: "Literal" }] }] };
    else if (path.endsWith("workflow-definitions/by-definition-id/slow-types")) json = definition;
    else if (path.endsWith("workflow-definitions") && route.request().method() === "POST") {
      definition = { ...definition, ...route.request().postDataJSON().model };
      saved = true;
      json = { workflowDefinition: definition };
    }
    await route.fulfill({ json });
  });
  try {
    await page.goto("/studio-preview/workflows/definitions/slow-types/edit");
    await page.locator('.react-flow__node[data-id="child"]').click();
    await expect.poll(() => !!pending).toBe(true);
    await page.locator(".cm-content").fill("2");
    await expect.poll(() => saved).toBe(true);
    await expect(page.getByText("Saved", { exact: true })).toBeVisible();
  } finally { release(); await pending; }
});
