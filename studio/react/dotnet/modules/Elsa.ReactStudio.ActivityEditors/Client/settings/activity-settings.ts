
import { translate } from "@elsa-workflows/react-studio-sdk/localization";
import type { Activity, Expression } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { WorkflowDefinitionSummary } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";

export interface PersistenceRule {
  evaluationMode: "Strategy" | "Expression" | 0 | 1;
  strategyType?: string | null;
  expression?: Expression | null;
  [key: string]: unknown;
}
export interface PersistenceConfiguration {
  default?: PersistenceRule | null;
  internalState?: PersistenceRule | null;
  inputs?: Record<string, PersistenceRule | null>;
  outputs?: Record<string, PersistenceRule | null>;
  [key: string]: unknown;
}
export interface ResilienceConfiguration {
  mode: "Identifier" | "Expression" | 0 | 1;
  strategyId?: string | null;
  expression?: Expression | null;
  [key: string]: unknown;
}

export function setCustomProperty(activity: Activity, name: string, value: unknown): Activity {
  const properties = { ...activity.customProperties };
  if (value === undefined) delete properties[name];
  else properties[name] = value;
  return { ...activity, customProperties: properties };
}

/** Read legacy input/output persistence modes without modifying a definition merely by opening it. */
export function persistenceConfiguration(activity: Activity): PersistenceConfiguration {
  const current = (activity.customProperties?.logPersistenceConfig ?? {}) as PersistenceConfiguration;
  const legacy = activity.customProperties?.logPersistenceMode as { default?: string | number; inputs?: Record<string, string | number>; outputs?: Record<string, string | number> } | undefined;
  const upgrade = (old: Record<string, string | number> = {}, next: Record<string, PersistenceRule | null> = {}) => ({
    ...Object.fromEntries(Object.entries(old).map(([name, mode]) => [name, { evaluationMode: "Strategy" as const, strategyType: legacyStrategy(mode) }])), ...Object.fromEntries(Object.entries(next).filter(([name, rule]) => rule != null || !(name in old))),
  });
  return { ...current, ...(current.default == null && legacy?.default !== undefined ? { default: { evaluationMode: "Strategy" as const, strategyType: legacyStrategy(legacy.default) } } : {}), inputs: upgrade(legacy?.inputs, current.inputs), outputs: upgrade(legacy?.outputs, current.outputs) };
}

export function setPersistenceRule(activity: Activity, section: "default" | "internalState" | "inputs" | "outputs",
  property: string | undefined, rule: PersistenceRule | undefined): Activity {
  const current = persistenceConfiguration(activity);
  const updated = rule ? activity : clearLegacyRule(activity, section, property);
  if (section === "inputs" || section === "outputs") {
    if (!property) throw new Error(translate("A property name is required for input/output persistence"));
    const name = property.charAt(0).toLowerCase() + property.slice(1);
    const next = { ...current[section] };
    // The runtime falls back from null to legacy data, so clear that one
    // legacy entry too. Preserve every unrelated legacy rule and custom field.
    next[name] = rule ?? null;
    return setCustomProperty(updated, "logPersistenceConfig", { ...current, [section]: next });
  }
  return setCustomProperty(updated, "logPersistenceConfig", { ...current, [section]: rule ?? null });
}

export function applyActivityVersion(activity: Activity, version: WorkflowDefinitionSummary): Activity {
  if (activity.workflowDefinitionId !== version.definitionId) throw new Error(translate("The version belongs to another workflow definition"));
  return { ...activity, version: version.version, workflowDefinitionVersionId: version.id };
}

function legacyStrategy(mode: string | number): string {
  const name = typeof mode === "number" ? ["Inherit", "Include", "Exclude"][mode] : mode;
  return ["Inherit", "Include", "Exclude"].includes(name) ? `Elsa.Workflows.LogPersistence.Strategies.${name}, Elsa.Workflows.Core` : String(mode);
}

function clearLegacyRule(activity: Activity, section: "default" | "internalState" | "inputs" | "outputs", property?: string): Activity {
  const legacy = activity.customProperties?.logPersistenceMode as Record<string, unknown> | undefined;
  if (!legacy) return activity;
  const next = { ...legacy };
  if ((section === "inputs" || section === "outputs") && property) {
    const rules = { ...(legacy[section] as Record<string, unknown> | undefined) };
    delete rules[property.charAt(0).toLowerCase() + property.slice(1)];
    next[section] = rules;
  } else delete next[section];
  return setCustomProperty(activity, "logPersistenceMode", next);
}
