import { expect, test } from "@playwright/test";

test.use({ timezoneId: "Europe/Amsterdam" });

test("specialized and extension UI hints edit their server values through registered slots", async ({ page }) => {
  await page.route("**/elsa/api/**", route => route.fulfill({ json: { items: [], totalCount: 0 } }));
  let originalEntry = "";
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch();
    const manifest = await response.json();
    const editors = manifest.modules.find((module: { id: string }) => module.id === "activity-editors");
    originalEntry = editors.entry;
    editors.entry = "/editors-fixture.js";
    await route.fulfill({ json: manifest });
  });
  await page.route("**/editors-fixture.js", route => route.fulfill({ contentType: "text/javascript", body: `
    import { createElement as h, useState } from "react";
    import { useExtensions } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
    import { register as registerEditors } from ${JSON.stringify(originalEntry)};
    const inputs = [{name:"Customer",type:"Object",displayName:"Customer record",description:"Preserved",storageDriverType:"Workflow",isArray:true}];
    function Fixture() {
      const hints = useExtensions("uiHints");
      const [readOnly, setReadOnly] = useState(false);
      const [values, setValues] = useState({
        date: {typeName:"DateTime",expression:{type:"Literal",value:"2026-10-20T15:30:45.000Z"}},
        outcomes: {typeName:"String[]",expression:{type:"Object",value:'["Approve"]'}},
        statuses: [{statusCode:200,activity:{id:"preserved-child",type:"Elsa.Sequence",version:1}},{statusCode:404}],
      });
      const context = {activityType:"Example.Activity",activityVersion:1,activity:{id:"fixture"},variables:[],outputs:[],inputs};
      const controls = [
        ["date","datetime-picker",{name:"Scheduled",typeName:"DateTime",defaultSyntax:"Literal"},["Literal"]],
        ["outcomes","dynamic-outcomes",{name:"Outcomes",typeName:"String[]",defaultSyntax:"Object",uiSpecifications:{DynamicOutcomesOptions:{fixedOutcomes:["Done"]}}},["Object"]],
        ["statuses","http-status-codes",{name:"ExpectedStatusCodes",typeName:"HttpStatusCodeCase[]",isWrapped:false},["Object"]],
        ["input","input-picker",{name:"Input",typeName:"InputDefinition",isWrapped:true},["Literal"]],
        ["email","singleline",{name:"Address",typeName:"String"},["Literal"]],
      ];
      return h("section",null,h("h1",null,"Editors fixture"),
        h("button",{onClick:()=>setReadOnly(value=>!value)},"Toggle read only"),
        ...controls.map(([id,hint,descriptor,syntaxes])=>h("div",{key:id,"data-testid":id},
          h(hints[hint],{descriptor,syntaxes,context,readOnly,value:values[id],onChange:next=>setValues(current=>({...current,[id]:next}))}),
          h("output",{"data-testid":id+"-value"},JSON.stringify(values[id])??"unset"))));
    }
    export function register(api) {
      registerEditors(api);
      api.contribute("uiHints",{id:"email-address",hint:"singleline",component:props=>h("input",{
        type:"email","aria-label":"Extension email",readOnly:props.readOnly,value:props.value?.expression?.value??"",
        onChange:event=>props.onChange({typeName:"String",expression:{type:"Literal",value:event.target.value}})})});
      api.contribute("routes",{id:"editors-fixture",path:"/editors-fixture",component:Fixture});
    }
  ` }));
  await page.goto("/studio-preview/editors-fixture");
  await expect(page.getByRole("heading", { name: "Editors fixture" })).toBeVisible();
  await expect(page.getByLabel("Scheduled time")).toHaveValue("17:30:45");
  await page.getByLabel("Scheduled date").fill("2026-10-21");
  await page.getByLabel("Scheduled time").fill("08:15:00");
  await expect(page.getByTestId("date-value")).toContainText("2026-10-21T06:15:00.000Z");
  await page.getByRole("button", { name: "Clear time", exact: true }).click();
  await expect(page.getByLabel("Scheduled time")).toHaveCount(0);
  await page.getByLabel("Scheduled date").fill("");
  await expect(page.getByTestId("date-value")).toHaveText(JSON.stringify({ typeName: "DateTime", expression: { type: "Literal", value: "" } }));

  await page.getByLabel("New outcome", { exact: true }).fill("Reject");
  await page.getByLabel("New outcome", { exact: true }).press("Enter");
  await page.getByRole("button", { name: "Remove outcome Approve" }).click();
  await expect(page.getByTestId("outcomes-value")).toHaveText(JSON.stringify({
    typeName: "String[]", expression: { type: "Object", value: '["Reject"]' },
  }));
  await expect(page.getByText("Done (fixed)", { exact: true })).toBeVisible();

  await page.getByLabel("New HTTP status code").fill("201");
  await page.getByLabel("New HTTP status code").press("Enter");
  await page.getByRole("button", { name: "Remove status 404" }).click();
  await expect(page.getByTestId("statuses-value")).toHaveText(JSON.stringify([
    { statusCode: 200, activity: { id: "preserved-child", type: "Elsa.Sequence", version: 1 } }, { statusCode: 201 },
  ]));
  await page.getByLabel("New HTTP status code").fill("600");
  await page.getByLabel("New HTTP status code").press("Enter");
  await expect(page.getByRole("alert").filter({ hasText: "100 to 599" })).toBeVisible();

  await page.getByTestId("input").getByRole("combobox").click();
  await page.getByRole("option", { name: "Customer — Customer record" }).click();
  await expect(page.getByTestId("input-value")).toHaveText(JSON.stringify({ typeName: "InputDefinition", expression: {
    type: "Input", value: JSON.stringify({ name: "Customer", type: "Object", displayName: "Customer record",
      description: "Preserved", storageDriverType: "Workflow", isArray: true }),
  } }));
  await page.getByLabel("Extension email").fill("person@example.com");
  await expect(page.getByTestId("email-value")).toContainText("person@example.com");
  await page.getByRole("button", { name: "Toggle read only" }).click();
  await expect(page.getByLabel("Scheduled date")).toHaveAttribute("readonly", "");
  await expect(page.getByTestId("input").getByRole("combobox")).toBeDisabled();
  await expect(page.getByLabel("New outcome", { exact: true })).toHaveCount(0);
  await expect(page.getByLabel("New HTTP status code")).toHaveCount(0);
});
