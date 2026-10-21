import { expect, it } from "vitest";
import { expressionCompletions, indexDataFlow } from "./data-flow.js";
import { ActivityRegistry } from "./registry.js";

it("offers C# properties only for names supported by generated runtime accessors", () => {
  const labels = expressionCompletions([{ name: "customer" }, { name: "customer_id" }], [{ name: "input_id" }], [], "csharp").map(item => item.label);
  expect(labels).toContain("Variable.Customer");
  expect(labels).toContain('Variable.Get("customer_id")');
  expect(labels).toContain('Input.Get<object>("input_id")');
  expect(labels).not.toContain("Variable.CustomerId");
  expect(labels).not.toContain("Input.input_id");
});

it("keeps direct JavaScript access while omitting unusable generated wrappers", () => {
  const labels = expressionCompletions([{ name: "customer_id" }], [{ name: "input_id" }], [], "javascript").map(item => item.label);
  expect(labels).toContain("variables.customer_id");
  expect(labels).toContain('getInput("input_id")');
  expect(labels).not.toContain("getCustomerId()");
  expect(labels).not.toContain("setCustomerId(value)");
  expect(labels).not.toContain("getinput_id()");
});

it.each([
  ["Variable.Customer.Name", "readers"],
  ["getCustomer().Name", "readers"],
  ['Variable.Get("customer").Name', "readers"],
  ['Variable.Get<Customer>("customer").Name', "readers"],
  ["Variables.customer.Name", "readers"],
  ["getcustomer().Name", "readers"],
  ["setCustomer(value)", "writers"],
  ['Variable.Set("customer", value)', "writers"],
  ["Variable.Customer = value", "writers"],
] as const)("indexes the emitted variable accessor %s", (value, side) => {
  const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
    { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type: "CSharp", value } } },
  ] }, [{ id: "customer-id", name: "customer" }], [], [], ActivityRegistry.Empty);
  expect(graph.byKey.get("variable:customer-id")?.[side]).toEqual([
    expect.objectContaining({ activityId: "step", property: "text", via: "expression" }),
  ]);
});

for (const property of ["Variable.Customer", "variables.customer"]) {
  it.each([
    [" = 42", 0, 1],
    [` = ${property} + 1`, 1, 1],
    [" == 42", 1, 0],
  ] as const)(`distinguishes reads and writes for ${property}%s`, (suffix, readers, writers) => {
    const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
      { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type: "CSharp", value: property + suffix } } },
    ] }, [{ id: "customer-id", name: "customer" }], [], [], ActivityRegistry.Empty);
    const usage = graph.byKey.get("variable:customer-id")!;
    expect(usage.readers).toHaveLength(readers);
    expect(usage.writers).toHaveLength(writers);
  });
}
