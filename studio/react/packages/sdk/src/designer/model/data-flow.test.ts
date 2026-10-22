import { expect, it } from "vitest";
import { expressionCompletions, indexDataFlow } from "./data-flow.js";
import { ActivityRegistry } from "./registry.js";

it.each([
  ["csharp", "variable", "Variable.Get"],
  ["csharp", "input", "Input.Get"],
  ["javascript", "input", "getInput"],
  ["python", "variable", "variables.get"],
  ["python", "input", "input.Get"],
] as const)("indexes the emitted %s %s getter for an escaped name", (language, kind, accessor) => {
  const name = 'customer "quoted"\\path\nline';
  const label = expressionCompletions([{ name }], [{ name }], [], language).find(item => item.label.startsWith(accessor))!.label;
  const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
    { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type: language === "csharp" ? "CSharp" : language === "python" ? "Python" : "JavaScript", value: label } } },
  ] }, [{ id: "customer-id", name }], [{ name, type: "String" }], [], ActivityRegistry.Empty);
  expect(graph.byKey.get(kind === "variable" ? "variable:customer-id" : `input:${name}`)?.readers).toHaveLength(1);
});

for (const type of ["Variable", "Input"] as const) {
  const reference = type === "Variable" ? { id: "customer-id", name: "customer" } : { name: "Customer", type: "String" };
  it.each([
    type === "Variable" ? "customer-id" : "Customer",
    ...(type === "Variable" ? ["customer"] : []),
    reference,
    JSON.stringify({ ...reference, description: "Example: variables.other" }),
  ])(`indexes a ${type} binding without evaluating its metadata: %j`, value => {
    const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
      { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type, value } } },
    ] }, [{ id: "customer-id", name: "customer" }, { id: "other-id", name: "other" }], [{ name: "Customer", type: "String" }], [], ActivityRegistry.Empty);
    const key = type === "Variable" ? "variable:customer-id" : "input:Customer";
    expect(graph.byKey.get(key)?.readers).toHaveLength(1);
    expect(graph.byKey.get("variable:other-id")?.readers).toHaveLength(0);
  });
}

it.each([
  { label: "raw id", value: "customer-id", readers: 1 },
  { label: "object", value: { id: "customer-id", name: "customer" }, readers: 1 },
  { label: "serialized object", value: JSON.stringify({ id: "customer-id", name: "customer" }), readers: 1 },
  { label: "missing explicit id", value: { id: "missing", name: "customer" }, readers: 0 },
])("honors the variable reference's exact id: $label", ({ value, readers }) => {
  const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
    { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type: "Variable", value } } },
  ] }, [{ id: "wrong-id", name: "customer-id" }, { id: "customer-id", name: "customer" }], [], [], ActivityRegistry.Empty);
  expect(graph.byKey.get("variable:customer-id")?.readers).toHaveLength(readers);
  expect(graph.byKey.get("variable:wrong-id")?.readers).toHaveLength(0);
});

it("ignores malformed or unsupported typed references without throwing", () => {
  for (const type of ["Variable", "Input"]) for (const value of [null, 42, [], "{bad", '[{"name":"Customer"}]', { id: 42, name: 42 }]) {
    const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
      { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type, value } } },
    ] }, [{ id: "customer-id", name: "customer" }], [{ name: "Customer", type: "String" }], [], ActivityRegistry.Empty);
    expect(graph.list.every(usage => usage.readers.length === 0 && usage.writers.length === 0)).toBe(true);
  }
});

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
      { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type: property.startsWith("variables.") ? "JavaScript" : "CSharp", value: property + suffix } } },
    ] }, [{ id: "customer-id", name: "customer" }], [], [], ActivityRegistry.Empty);
    const usage = graph.byKey.get("variable:customer-id")!;
    expect(usage.readers).toHaveLength(readers);
    expect(usage.writers).toHaveLength(writers);
  });
}

for (const property of ["Variable.Customer", "variables.customer"]) {
  const operators = ["+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=", "??="];
  if (property.startsWith("variables.")) operators.push("**=", "&&=", "||=");
  const updates = [
    ...operators.map(operator => `${property} ${operator} 1`),
    `${property}++`, `${property}--`, `++${property}`, `--${property}`,
  ];
  it.each(updates)("indexes compound variable mutations as reads and writes: %s", value => {
    const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
      { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type: property.startsWith("variables.") ? "JavaScript" : "CSharp", value } } },
    ] }, [{ id: "customer-id", name: "customer" }], [], [], ActivityRegistry.Empty);
    const usage = graph.byKey.get("variable:customer-id")!;
    expect(usage.readers).toHaveLength(1);
    expect(usage.writers).toHaveLength(1);
  });
  it.each([" >= 42", " <= 42", " != 42"])(`keeps ${property}%s as a read without a write`, suffix => {
    const graph = indexDataFlow({ id: "root", type: "Elsa.Sequence", version: 1, activities: [
      { id: "step", type: "Elsa.WriteLine", version: 1, text: { expression: { type: property.startsWith("variables.") ? "JavaScript" : "CSharp", value: property + suffix } } },
    ] }, [{ id: "customer-id", name: "customer" }], [], [], ActivityRegistry.Empty);
    const usage = graph.byKey.get("variable:customer-id")!;
    expect(usage.readers).toHaveLength(1);
    expect(usage.writers).toHaveLength(0);
  });
}
