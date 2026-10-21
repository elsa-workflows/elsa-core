import { expect, it } from "vitest";
import { parseServerTypes, typedCompletions } from "../type-completions";

const document = `declare function getServerValue(): Customer;
declare class Customer {
Name: string;
Age?: number;
Greet: (message: string) => string;
}
declare var variables: WorkflowVariables;
declare class WorkflowVariables {
Removed: Customer;
}
// unknown syntax must never be executed
globalThis.throwIfExecuted();`;
const context = { variables: [{ id: "customer", name: "customer", typeName: "Customer" }], inputs: [{ name: "Customer", type: "Customer" }], activities: [{ name: 'Read"Customer', outputs: ["Result"] }] };

for (const [language, member, input, output] of [
  ["javascript", "variables.customer.Name", 'getInput("Customer")', 'getOutputFrom("Read\\\"Customer", "Result")'],
  ["csharp", "Variable.Customer.Name", "Input.Customer.Age", 'Output.From("Read\\\"Customer", "Result")'],
  ["python", "variables.customer.Name", 'input.Get("Customer").Age', 'output.Get("Read\\\"Customer", "Result")'],
] as const) it(`${language} completes current workflow members with server type information`, () => {
  const items = typedCompletions(context, language, parseServerTypes(document));
  const labels = items.map(item => item.label);
  expect(labels).toEqual(expect.arrayContaining([member, input, output]));
  expect(items.find(item => item.label === member)?.detail).toBe("string");
  expect(labels).not.toContain("variables.Removed");
  if (language === "javascript") expect(labels).toContain("getServerValue");
  else expect(labels).not.toContain("getServerValue");
});

it("keeps local members when server information is absent and retains Liquid support", () => {
  expect(typedCompletions(context, "csharp", parseServerTypes("")).map(item => item.label)).toContain("Variable.Customer");
  expect(typedCompletions(context, "liquid", parseServerTypes("")).map(item => item.label)).toContain("Variables.customer");
});

it("handles a large workflow without dropping its last activity output", () => {
  const large = { variables: Array.from({ length: 3000 }, (_, index) => ({ id: String(index), name: `value${index}`, typeName: "Customer" })),
    inputs: Array.from({ length: 3000 }, (_, index) => ({ name: `Input${index}`, type: "String" })),
    activities: Array.from({ length: 10000 }, (_, index) => ({ name: `Activity${index}`, outputs: ["Result", "Value"] })) };
  const labels = typedCompletions(large, "csharp", parseServerTypes(document)).map(item => item.label);
  expect(labels).toContain('Output.From("Activity9999", "Value")');
  expect(labels).toContain("Variable.Value2999.Greet");
  expect(new Set(labels).size).toBe(labels.length);
});
