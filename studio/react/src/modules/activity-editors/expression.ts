import type { ActivityInput, Expression } from "@/lib/api/types/activities";
import type { InputDescriptor } from "@/lib/api/types/descriptors";
import type { CodeLanguage } from "@/components/common/code-editor";

export const LITERAL = "Literal";
export const OBJECT = "Object";
/** Binds an input to a workflow variable; the stored value is the `Variable` object itself. */
export const VARIABLE = "Variable";
/** Binds an input to a workflow input; the stored value is the `InputDefinition` object. */
export const INPUT = "Input";

/** Expression types that are not user-selectable syntaxes (no editor, or encoded implicitly). */
const HIDDEN_SYNTAXES = new Set(["Delegate", "Json", "Secret"]);

/** Syntaxes bound to a named workflow member rather than to text, edited with a picker. */
export function isBindingSyntax(syntax: string | undefined): syntax is typeof VARIABLE | typeof INPUT {
  return syntax === VARIABLE || syntax === INPUT;
}

export function selectableSyntaxes(all: { type: string; isBrowsable: boolean }[]): string[] {
  const names = all.filter((d) => d.isBrowsable && !HIDDEN_SYNTAXES.has(d.type)).map((d) => d.type);
  return [LITERAL, ...names.filter((n) => n !== LITERAL)];
}

export function languageForSyntax(syntax: string | undefined, fallback: CodeLanguage = "plaintext"): CodeLanguage {
  switch (syntax) {
    case "JavaScript":
      return "javascript";
    case "CSharp":
      return "csharp";
    case "Python":
      return "python";
    case "Liquid":
      return "liquid";
    case "Object":
    case "Json":
      return "json";
    default:
      return fallback;
  }
}

/** Reads the literal value of a wrapped input (or the raw value for non-wrapped inputs). */
export function literalValueOf(input: ActivityInput | unknown): unknown {
  if (input && typeof input === "object" && "expression" in (input as object)) {
    const expr = (input as ActivityInput).expression;
    return expr?.type === LITERAL || expr?.type === OBJECT || expr?.type === "Json" ? expr.value : undefined;
  }
  return input;
}

export function syntaxOf(input: ActivityInput | undefined, descriptor: InputDescriptor): string {
  return input?.expression?.type ?? descriptor.defaultSyntax ?? LITERAL;
}

export function makeInput(descriptor: InputDescriptor, expression: Expression): ActivityInput {
  return { typeName: descriptor.typeName, expression };
}

/**
 * Reads a list-valued input as plain strings.
 *
 * Elsa stores lists under the `Object` syntax as a JSON string (`"[\"GET\",\"POST\"]"`), and
 * normalises an array back into that same string on save, so both shapes have to be accepted. The
 * descriptor's default value (also a JSON string) applies only when the input was never set, so
 * clearing every item stays cleared instead of snapping back to the defaults.
 */
export function stringListOf(value: unknown, descriptor?: InputDescriptor): string[] {
  if (value === undefined || value === null) return toStringList(descriptor?.defaultValue);
  return toStringList(value);
}

function toStringList(value: unknown): string[] {
  if (Array.isArray(value)) return value.map(String);
  if (typeof value !== "string") return [];
  const text = value.trim();
  if (!text) return [];
  try {
    const parsed: unknown = JSON.parse(text);
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch {
    return [];
  }
}

export function expressionText(expression: Expression | undefined): string {
  const v = expression?.value;
  if (v === undefined || v === null) return "";
  if (typeof v === "string") return v;
  return JSON.stringify(v, null, 2);
}
