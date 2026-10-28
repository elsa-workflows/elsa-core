/** Workflow-level log persistence mode stored on a definition's custom properties. */
export function workflowLogPersistenceMode(customProperties: Record<string, unknown> | undefined): string {
  const configured = readConfiguration(customProperties?.logPersistenceConfig);
  if (configured) return configured;
  return readLegacy(customProperties?.logPersistenceMode) ?? "Inherit";
}

function readConfiguration(value: unknown): string | undefined {
  if (!hasProperty(value, "default")) return undefined;
  const rule = property(value, "default");
  // A null default falls through to the legacy mode, matching the runtime.
  if (rule == null) return undefined;
  if (typeof rule !== "object") return "Inherit";

  const record = rule as Record<string, unknown>;
  const mode = record.evaluationMode;
  if (mode === "Expression" || mode === 1) {
    const text = expressionText(record.expression);
    if (text) return text;
  }
  return shortName(record.strategyType) ?? "Inherit";
}

function readLegacy(value: unknown): string | undefined {
  const mode = property(value, "default");
  if (typeof mode === "string" && mode.trim()) return mode.includes(".") ? shortName(mode) ?? mode.trim() : mode.trim();
  if (typeof mode === "number" && Number.isInteger(mode)) return ["Inherit", "Include", "Exclude"][mode];
  return undefined;
}

function expressionText(expression: unknown): string | undefined {
  if (!expression || typeof expression !== "object") return undefined;
  const value = (expression as { value?: unknown }).value;
  if (typeof value === "string" && value.trim()) return value.trim();
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
