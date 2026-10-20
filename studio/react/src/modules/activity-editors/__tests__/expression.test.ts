import { describe, expect, it } from "vitest";
import type { InputDescriptor } from "@/lib/api/types/descriptors";
import { isBindingSyntax, stringListOf } from "../expression";

const descriptor = (defaultValue?: unknown) => ({ name: "SupportedMethods", typeName: "List<String>", defaultValue } as unknown as InputDescriptor);

describe("stringListOf", () => {
  it("reads a plain array, preserving multiple values and their order", () => {
    expect(stringListOf(["GET", "POST"], descriptor())).toEqual(["GET", "POST"]);
  });

  it("reads the JSON string Elsa normalises lists into, with multiple values", () => {
    expect(stringListOf('["GET","POST"]', descriptor())).toEqual(["GET", "POST"]);
    expect(stringListOf('["POST"]', descriptor())).toEqual(["POST"]);
  });

  it("coerces non-string items to strings", () => {
    expect(stringListOf([1, 2], descriptor())).toEqual(["1", "2"]);
    expect(stringListOf("[1,2]", descriptor())).toEqual(["1", "2"]);
  });

  it("returns nothing for an empty, blank or unparsable value", () => {
    expect(stringListOf("", descriptor())).toEqual([]);
    expect(stringListOf("   ", descriptor())).toEqual([]);
    expect(stringListOf("not json", descriptor())).toEqual([]);
    expect(stringListOf('"POST"', descriptor())).toEqual([]); // valid JSON, but not a list
    expect(stringListOf(42, descriptor())).toEqual([]);
  });

  it("falls back to the descriptor default only when the input was never set", () => {
    expect(stringListOf(undefined, descriptor('["GET"]'))).toEqual(["GET"]);
    expect(stringListOf(null, descriptor(["GET"]))).toEqual(["GET"]);
    // Clearing every item must stay cleared rather than snapping back to the defaults.
    expect(stringListOf([], descriptor('["GET"]'))).toEqual([]);
    expect(stringListOf("[]", descriptor('["GET"]'))).toEqual([]);
  });

  it("works without a descriptor", () => {
    expect(stringListOf('["GET","POST"]')).toEqual(["GET", "POST"]);
    expect(stringListOf(undefined)).toEqual([]);
  });
});

describe("isBindingSyntax", () => {
  it("recognises the syntaxes that bind to a workflow member", () => {
    expect(isBindingSyntax("Variable")).toBe(true);
    expect(isBindingSyntax("Input")).toBe(true);
  });

  it("rejects text and literal syntaxes", () => {
    for (const s of ["Literal", "Object", "JavaScript", "Liquid", "Json", undefined]) expect(isBindingSyntax(s)).toBe(false);
  });
});
