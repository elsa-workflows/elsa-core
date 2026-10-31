/** Workflow-level log persistence stored on a definition's custom properties. */
export type LogPersistenceDisplay = {
  kind: "mode" | "expression";
  text: string;
};

export function workflowLogPersistenceMode(customProperties: Record<string, unknown> | undefined): LogPersistenceDisplay {
  const configured = readConfiguration(customProperties?.logPersistenceConfig);
  if (configured) return configured;
  return readLegacy(customProperties?.logPersistenceMode) ?? mode("Inherit");
}

function readConfiguration(value: unknown): LogPersistenceDisplay | undefined {
  if (!hasProperty(value, "default")) return undefined;
  const rule = property(value, "default");
  // A null default falls through to the legacy mode, matching the runtime.
  if (rule == null) return undefined;
  if (typeof rule !== "object") return mode("Inherit");

  const record = rule as Record<string, unknown>;
  const evaluation = record.evaluationMode;
  if (evaluation === "Expression" || evaluation === 1) {
    // Expression mode ignores strategyType. A blank expression inherits.
    const text = expressionText(record.expression);
    return text ? expression(text) : mode("Inherit");
  }
  return mode(shortName(record.strategyType) ?? "Inherit");
}

function readLegacy(value: unknown): LogPersistenceDisplay | undefined {
  const legacy = property(value, "default");
  if (typeof legacy === "string" && legacy.trim()) {
    const trimmed = legacy.trim();
    return mode(trimmed.includes(".") ? shortName(trimmed) ?? trimmed : trimmed);
  }
  if (typeof legacy === "number" && Number.isInteger(legacy)) {
    const name = ["Inherit", "Include", "Exclude"][legacy];
    return name ? mode(name) : undefined;
  }
  return undefined;
}

function expressionText(expression: unknown): string | undefined {
  if (!expression || typeof expression !== "object") return undefined;
  const value = (expression as { value?: unknown }).value;
  if (typeof value === "string") {
    const trimmed = value.trim();
    return trimmed ? trimmed : undefined;
  }
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  return undefined;
}

function shortName(typeName: unknown): string | undefined {
  if (typeof typeName !== "string" || !typeName.trim()) return undefined;
  const type = typeName.split(",")[0]?.trim() ?? "";
  const parts = type.split(".").filter(Boolean);
  return parts.at(-1);
}

function hasProperty(value: unknown, name: string): boolean {
  return !!value && typeof value === "object" && name in value;
}

function property(value: unknown, name: string): unknown {
  if (!hasProperty(value, name)) return undefined;
  return (value as Record<string, unknown>)[name];
}

function mode(text: string): LogPersistenceDisplay {
  return { kind: "mode", text };
}

function expression(text: string): LogPersistenceDisplay {
  return { kind: "expression", text };
}
