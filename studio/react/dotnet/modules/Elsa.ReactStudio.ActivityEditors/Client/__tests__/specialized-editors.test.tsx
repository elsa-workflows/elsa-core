import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import type { InputDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import type { InputDefinition } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import type { UiHintEditorProps } from "@elsa-workflows/react-studio-sdk";
import { StudioRegistry } from "@elsa-workflows/react-studio-sdk/host";
import { computePorts } from "@elsa-workflows/react-studio-sdk/designer/model/ports";
import type { ActivityDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import { activityEditorsModule } from "../module";
import { DateTimePickerEditor } from "../editors/datetime-picker-editor";
import { DynamicOutcomesEditor } from "../editors/dynamic-outcomes-editor";
import { HttpStatusCodesEditor } from "../editors/http-status-codes-editor";
import { InputPickerEditor } from "../editors/input-picker-editor";
import { dateTimeParts, dateTimeValue, fixedOutcomes, inputSelection, parseStatusCode, selectedInput, serializeOutcomes,
  statusCodes, statusCodeValue } from "../editors/editor-values";

const descriptor = (patch: Partial<InputDescriptor> = {}): InputDescriptor => ({ name: "Value", typeName: "Object", ...patch });
const props = (input: InputDescriptor, value?: UiHintEditorProps["value"]): UiHintEditorProps => ({
  descriptor: input, value, onChange() {}, syntaxes: ["Object"], readOnly: true,
  context: { activityType: "Example.Activity", activityVersion: 1, activity: {}, variables: [], outputs: [] },
});

describe("DateTimePicker", () => {
  it("preserves a saved instant through local date/time controls, including seconds, and clears explicitly", () => {
    const instant = "2026-10-20T12:34:56.000Z";
    const parts = dateTimeParts(instant);
    expect(dateTimeValue(parts.date, parts.time)).toBe(instant);
    expect(dateTimeValue("", parts.time)).toBe("");
    expect(dateTimeParts("invalid")).toEqual({ date: "", time: "" });
    const markup = renderToStaticMarkup(<DateTimePickerEditor {...props(descriptor({ defaultSyntax: "Literal" }), {
      typeName: "DateTime", expression: { type: "Literal", value: instant },
    })} syntaxes={["Literal"]} />);
    expect(markup).toContain('type="date"');
    expect(markup).toContain('type="time"');
    expect(markup).toContain('readOnly=""');
    expect(markup).toContain('disabled=""');
  });
});

describe("DynamicOutcomes", () => {
  const input = descriptor({ uiHint: "dynamic-outcomes", uiSpecifications: { DynamicOutcomesOptions: { fixedOutcomes: ["Done"] } } });
  it("writes the server's Object JSON list, excludes fixed ports, and keeps an emptied list empty", () => {
    expect(serializeOutcomes([" Approve ", "Reject", "Approve", "Done", ""], input)).toBe('["Approve","Reject"]');
    expect(serializeOutcomes([], input)).toBe("[]");
    expect(fixedOutcomes(input)).toEqual(["Done"]);
    const value = { typeName: "String[]", expression: { type: "Object", value: '["Approve"]' } };
    const markup = renderToStaticMarkup(<DynamicOutcomesEditor {...props(input, value)} />);
    expect(markup).toContain("Approve");
    expect(markup).toContain("Done (fixed)");
    expect(markup).not.toContain("New outcome");
    const activity = { id: "choice", type: "Example.Decision", version: 1, value };
    const ports = computePorts(activity, { inputs: [input], ports: [] } as unknown as ActivityDescriptor);
    expect(ports.outPorts.map(port => port.name)).toEqual(["Approve", "Done"]);
  });
});

describe("HttpStatusCodes", () => {
  it("preserves nested activity data when adding/removing cases and writes numeric arrays for wrapped HTTP inputs", () => {
    const cases = [{ statusCode: 200, activity: { id: "keep", type: "Elsa.WriteLine", version: 1 }, future: "retain" }, { statusCode: 404 }];
    const raw = descriptor({ isWrapped: false, typeName: "HttpStatusCodeCase[]" });
    expect(statusCodeValue(cases, [200, 201, 201], raw)).toEqual([cases[0], { statusCode: 201 }]);
    expect(cases).toHaveLength(2);
    expect(statusCodeValue('[200,404]', [200, 503], descriptor({ typeName: "Int32[]" }))).toBe("[200,503]");
    expect(statusCodes('[200,"404"]')).toEqual([200, 404]);
    expect(parseStatusCode("599")).toBe(599);
    for (const text of ["99", "600", "200.1", "200x", "NaN"]) expect(parseStatusCode(text)).toBeUndefined();
    const markup = renderToStaticMarkup(<HttpStatusCodesEditor {...props(raw, cases as unknown as UiHintEditorProps["value"])} />);
    expect(markup).toContain("200");
    expect(markup).not.toContain("New HTTP status code");
    expect(markup).not.toContain("Remove status");
  });
});

describe("InputPicker", () => {
  it("round-trips the complete input definition for both server encodings and permits clearing", () => {
    const definition: InputDefinition = { name: "Customer", type: "Object", displayName: "Customer record",
      description: "Keep metadata", storageDriverType: "Workflow", isArray: true, uiHint: "json-editor" };
    const wrapped = descriptor({ typeName: "InputDefinition", isWrapped: true });
    const raw = descriptor({ typeName: "InputDefinition", isWrapped: false });
    expect(inputSelection(definition, wrapped)?.expression).toEqual({ type: "Input", value: JSON.stringify(definition) });
    expect(selectedInput(inputSelection(definition, wrapped), wrapped)).toEqual(definition);
    expect(selectedInput(inputSelection(definition, raw), raw)).toEqual(definition);
    expect(inputSelection(undefined, wrapped)).toBeUndefined();
    expect(selectedInput({ expression: { value: "malformed" } }, wrapped)).toBeUndefined();
    const settings = props(wrapped, inputSelection(definition, wrapped));
    settings.context.inputs = [definition];
    const markup = renderToStaticMarkup(<InputPickerEditor {...settings} />);
    expect(markup).toContain("Customer record");
    expect(markup).toContain('disabled=""');
  });
});

it("registers the four specialized editors and extension hints through the same public slot", async () => {
  const registry = new StudioRegistry();
  await registry.load({ id: "activity-editors", version: "1", hostApiVersion: 1, sdkApiVersion: 1, entry: "editors.js" },
    async () => ({ register: activityEditorsModule.register }));
  await registry.load({ id: "email-extension", version: "1", hostApiVersion: 1, sdkApiVersion: 1, entry: "email.js" }, async () => ({
    register(api) {
      api.contribute("uiHints", { id: "email-address", hint: "email-address", component: () => <input type="email" /> });
      api.contribute("uiHints", { id: "lower-priority", hint: "email-address", order: 10, component: () => <p>Lower priority</p> });
    },
  }));
  expect(registry.slots("uiHints").map(slot => slot.hint)).toEqual(expect.arrayContaining([
    "datetime-picker", "dynamic-outcomes", "http-status-codes", "input-picker", "email-address",
  ]));
  const Email = registry.studioModules().find(module => module.id === "email-extension")!.extensions!.uiHints!["email-address"];
  expect(renderToStaticMarkup(<Email {...props(descriptor())} />)).toContain('type="email"');
});


it("lets a default-order external hint override a built-in even when its owner sorts later", async () => {
  const registry = new StudioRegistry();
  await registry.load({ id: "activity-editors", version: "1", hostApiVersion: 1, sdkApiVersion: 1, entry: "editors.js" },
    async () => ({ register: activityEditorsModule.register }));
  await registry.load({ id: "z-custom", version: "1", hostApiVersion: 1, sdkApiVersion: 1, entry: "custom.js" }, async () => ({
    register(api) { api.contribute("uiHints", { id: "override", hint: "singleline", component: () => <input type="email" /> }); },
  }));
  const winner = registry.slots("uiHints").find(slot => slot.hint === "singleline")!;
  expect(renderToStaticMarkup(<winner.component {...props(descriptor())} />)).toContain('type="email"');
});
