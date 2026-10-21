import type { Activity } from "@elsa-workflows/react-studio-api-client/types/activities";
import { isFlowchart, isSequence } from "@elsa-workflows/react-studio-api-client/types/activities";
import { childActivities } from "./containers.js";
import type { InputDefinition, OutputDefinition } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import type { ActivityRegistry } from "./registry.js";
import { translate, type Translate } from "../../localization.js";

/** A named piece of workflow data that activities can read or write. */
export type DataKind = "variable" | "input" | "output";

export interface DataRef {
  kind: DataKind;
  /** Variable id, or the input/output name. */
  key: string;
  name: string;
}

export interface DataUse {
  activityId: string;
  activityName: string;
  /** The activity property (input or output) where the reference was found. */
  property: string;
  /** How the reference was expressed, for display. */
  via: "binding" | "SetVariable" | "SetOutput" | "expression";
}

export interface DataUsage extends DataRef {
  writers: DataUse[];
  readers: DataUse[];
}

export interface ActivityDataSummary {
  reads: DataRef[];
  writes: DataRef[];
}

/** Everything computed in one pass so panels, nodes and the overlay share the same answer. */
export interface DataFlowIndex {
  byKey: Map<string, DataUsage>;
  byActivity: Map<string, ActivityDataSummary>;
  list: DataUsage[];
}

function escapeRe(s: string) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// Generated C#/JavaScript wrappers accept alphanumeric names only. An underscore
// is a valid language identifier but is rejected by the runtime name validator.
const generatedAccessor = /^[A-Za-z][A-Za-z0-9]*$/;
const pascal = (name: string) => name.replace(/(?:^|_)([a-z])/g, (_, letter: string) => letter.toUpperCase());

/**
 * Patterns under which expressions reference a variable in Elsa 3:
 * Includes direct containers, runtime-generated PascalCase aliases and generic
 * getters/setters. Property mutations and setter calls identify writes.
 */
function variablePatterns(name: string) {
  const n = escapeRe(name);
  const argument = `(?:${escapeRe(JSON.stringify(name))}|['\`]${n}['\`])`;
  const properties = [`\\b[vV]ariables\\.${n}\\b`];
  const getters = [`\\bget${n}\\s*\\(`];
  const setters = [`\\bset${n}\\s*\\(`];
  if (generatedAccessor.test(name)) {
    const alias = escapeRe(pascal(name));
    properties.push(`\\bVariables?\\.${alias}\\b`);
    getters.push(`\\bget${alias}\\s*\\(`);
    setters.push(`\\bset${alias}\\s*\\(`);
  }
  const property = `(?:${properties.join("|")})`;
  const assignment = String.raw`(?:=(?!=|>)|(?:\*\*|>>>?|<<|\?\?|&&|\|\||[+\-*/%&|^])=)`;
  const update = String.raw`(?:\+\+|--)`;
  return {
    read: new RegExp(`${property}(?!\\s*=(?!=))|${getters.join("|")}|\\bgetVariable\\s*\\(\\s*${argument}|\\b(?:Variables?\\.Get(?:<[^>]+>)?|variables\\.get)\\s*\\(\\s*${argument}`),
    write: new RegExp(`${property}\\s*(?:${assignment}|${update})|${update}\\s*${property}|${setters.join("|")}|\\bsetVariable\\s*\\(\\s*${argument}|\\bVariables?\\.Set\\s*\\(\\s*${argument}`),
  };
}

/** JS `input.Name` / `getName()` / `getInput('Name')`, Liquid/C# `Input.Name`, `Inputs.Name`. */
function inputPattern(name: string) {
  const n = escapeRe(name);
  const argument = `(?:${escapeRe(JSON.stringify(name))}|['\`]${n}['\`])`;
  return new RegExp(`\\b[iI]nputs?\\.${n}\\b|\\bget${n}\\s*\\(|\\bgetInput\\s*\\(\\s*${argument}|\\b[iI]nputs?\\.Get(?:<[^>]+>)?\\s*\\(\\s*${argument}`);
}

/** Pickers can store a reference object or its JSON serialization; older models use a raw id/name. */
function bindingReference(value: unknown): string | { id?: string; name?: string } | undefined {
  if (typeof value === "string") {
    const text = value;
    try {
      const parsed: unknown = JSON.parse(text);
      if (parsed && typeof parsed === "object" && !Array.isArray(parsed)) value = parsed;
      else return text;
    } catch {
      return text;
    }
  }
  if (!value || typeof value !== "object" || Array.isArray(value)) return undefined;
  const reference = value as Record<string, unknown>;
  return {
    id: typeof reference.id === "string" ? reference.id : undefined,
    name: typeof reference.name === "string" ? reference.name : undefined,
  };
}

/** Collects every string that could be an expression inside an activity's properties (one level deep is enough: wrapped inputs). */
function expressionTexts(activity: Activity): { property: string; text: string }[] {
  const out: { property: string; text: string }[] = [];
  for (const [prop, value] of Object.entries(activity)) {
    if (["id", "nodeId", "type", "version", "name", "metadata", "customProperties"].includes(prop)) continue;
    if (!value || typeof value !== "object") continue;
    if (typeof (value as Activity).type === "string" && typeof (value as Activity).id === "string") continue; // a child activity, not an input
    const wrapped = value as { expression?: { type?: string; value?: unknown } };
    const expr = wrapped.expression;
    if (expr && typeof expr.value === "string" && !["Literal", "Object", "Variable", "Input"].includes(expr.type ?? "")) out.push({ property: prop, text: expr.value });
    // Switch / FlowSwitch cases carry their own expressions.
    if (Array.isArray(value)) {
      for (const item of value as { condition?: { value?: unknown } }[]) {
        if (item && typeof item.condition?.value === "string") out.push({ property: prop, text: item.condition.value });
      }
    }
    const literal = expr?.type === "Literal" || expr?.type === "Object" ? expr.value : undefined;
    if (Array.isArray(literal)) {
      for (const item of literal as { condition?: { value?: unknown } }[]) {
        if (item && typeof item.condition?.value === "string") out.push({ property: prop, text: item.condition.value });
      }
    }
  }
  return out;
}

export function indexDataFlow(
  root: Activity,
  variables: { id: string; name: string }[],
  inputs: InputDefinition[],
  outputs: OutputDefinition[],
  registry: ActivityRegistry,
): DataFlowIndex {
  const byKey = new Map<string, DataUsage>();
  const byActivity = new Map<string, ActivityDataSummary>();
  const refKey = (kind: DataKind, key: string) => `${kind}:${key}`;

  const allVariables = [...variables];
  const visitForVariables = (a: Activity) => {
    if (isFlowchart(a) || isSequence(a)) for (const v of a.variables ?? []) if (!allVariables.some((x) => x.id === v.id)) allVariables.push({ id: v.id, name: v.name });
    for (const c of childActivities(a)) visitForVariables(c);
  };
  visitForVariables(root);

  for (const v of allVariables) byKey.set(refKey("variable", v.id), { kind: "variable", key: v.id, name: v.name, writers: [], readers: [] });
  for (const i of inputs) byKey.set(refKey("input", i.name), { kind: "input", key: i.name, name: i.name, writers: [], readers: [] });
  for (const o of outputs) byKey.set(refKey("output", o.name), { kind: "output", key: o.name, name: o.name, writers: [], readers: [] });

  const note = (usage: DataUsage, side: "writers" | "readers", use: DataUse) => {
    if (usage[side].some((u) => u.activityId === use.activityId && u.property === use.property)) return;
    usage[side].push(use);
    const summary = byActivity.get(use.activityId) ?? { reads: [], writes: [] };
    const list = side === "writers" ? summary.writes : summary.reads;
    if (!list.some((r) => r.kind === usage.kind && r.key === usage.key)) list.push({ kind: usage.kind, key: usage.key, name: usage.name });
    byActivity.set(use.activityId, summary);
  };

  const visit = (parent: Activity) => {
    for (const a of childActivities(parent)) {
      const descriptor = registry.get(a.type, a.version);
      const activityName = a.name ?? descriptor?.displayName ?? a.type;
      const base = { activityId: a.id, activityName };

      // Output bindings: activity output stored into a variable.
      for (const output of descriptor?.outputs ?? []) {
        const prop = output.name.charAt(0).toLowerCase() + output.name.slice(1);
        const bound = (a[prop] as { memoryReference?: { id?: string } } | undefined)?.memoryReference?.id;
        const usage = bound ? byKey.get(refKey("variable", bound)) : undefined;
        if (usage) note(usage, "writers", { ...base, property: output.name, via: "binding" });
      }

      // SetVariable / SetOutput pick their target as a literal.
      if (a.type === "Elsa.SetVariable") {
        const target = (a.variable as { expression?: { value?: { id?: string } } } | undefined)?.expression?.value?.id;
        const usage = target ? byKey.get(refKey("variable", target)) : undefined;
        if (usage) note(usage, "writers", { ...base, property: "Variable", via: "SetVariable" });
      }
      if (a.type === "Elsa.SetOutput") {
        const target = (a.outputName as { expression?: { value?: unknown } } | undefined)?.expression?.value;
        const usage = typeof target === "string" ? byKey.get(refKey("output", target)) : undefined;
        if (usage) note(usage, "writers", { ...base, property: "OutputName", via: "SetOutput" });
      }

      // Expressions anywhere in the activity.
      const texts = expressionTexts(a);
      for (const { property, text } of texts) {
        for (const v of allVariables) {
          const p = variablePatterns(v.name);
          const usage = byKey.get(refKey("variable", v.id))!;
          if (p.write.test(text)) note(usage, "writers", { ...base, property, via: "expression" });
          if (p.read.test(text)) note(usage, "readers", { ...base, property, via: "expression" });
        }
        for (const i of inputs) {
          if (inputPattern(i.name).test(text)) note(byKey.get(refKey("input", i.name))!, "readers", { ...base, property, via: "expression" });
        }
      }

      // "Variable" / "Input" typed expressions reference data directly.
      for (const [prop, value] of Object.entries(a)) {
        const expr = (value as { expression?: { type?: string; value?: unknown } } | undefined)?.expression;
        if (!expr || typeof expr !== "object") continue;
        if (expr.type === "Variable") {
          const reference = bindingReference(expr.value);
          const id = typeof reference === "string" ? reference : reference?.id;
          const usage = allVariables.find((v) => v.id === id) ?? (typeof reference === "string" ? allVariables.find((v) => v.name === reference) : undefined);
          if (usage) note(byKey.get(refKey("variable", usage.id))!, "readers", { ...base, property: prop, via: "expression" });
        }
        if (expr.type === "Input") {
          const reference = bindingReference(expr.value);
          const name = typeof reference === "string" ? reference : reference?.name;
          const usage = name ? byKey.get(refKey("input", name)) : undefined;
          if (usage) note(usage, "readers", { ...base, property: prop, via: "expression" });
        }
      }

      visit(a);
    }
  };
  visit(root);

  return { byKey, byActivity, list: [...byKey.values()] };
}

export function dataRefKey(ref: Pick<DataRef, "kind" | "key">) {
  return `${ref.kind}:${ref.key}`;
}

/**
 * Names offered by the expression autocompleter for a given context: `variables.X`, `getX()`,
 * `getInput('X')`, plus `getOutputFrom('Activity', 'Output')` for activities with outputs.
 */
export function expressionCompletions(
  variables: { name: string }[],
  inputs: { name: string }[],
  activities: { name: string; outputs: string[] }[],
  language: "javascript" | "csharp" | "python" | "liquid" | "plaintext",
  t: Translate = translate,
): { label: string; detail: string; type: string }[] {
  const items: { label: string; detail: string; type: string }[] = [];
  const add = (label: string, detail: string, type = "variable") => items.push({ label, detail, type });
  const identifier = /^[A-Za-z_][\w]*$/;
  const quote = (value: string) => JSON.stringify(value);
  const details = { variable: t("variable"), getter: t("variable getter"), setter: t("variable setter"), input: t("workflow input") };
  for (const v of variables) {
    if ((language === "javascript" || language === "python") && identifier.test(v.name)) add(`variables.${v.name}`, details.variable);
    if (language === "csharp") {
      if (generatedAccessor.test(v.name)) add(`Variable.${pascal(v.name)}`, details.variable);
      add(`Variable.Get(${quote(v.name)})`, details.variable, "function");
    }
    if (language === "liquid" && identifier.test(v.name)) add(`Variables.${v.name}`, details.variable);
    if (language === "python") add(`variables.get(${quote(v.name)})`, details.variable, "function");
    if (language === "javascript" && generatedAccessor.test(v.name)) {
      add(`get${pascal(v.name)}()`, details.getter, "function");
      add(`set${pascal(v.name)}(value)`, details.setter, "function");
    }
  }
  for (const i of inputs) {
    if (language === "javascript") {
      if (generatedAccessor.test(i.name)) add(`get${i.name}()`, details.input, "function");
      add(`getInput(${quote(i.name)})`, details.input, "function");
    }
    if ((language === "csharp" && generatedAccessor.test(i.name)) || (language === "liquid" && identifier.test(i.name))) add(`Input.${i.name}`, details.input);
    if (language === "csharp") add(`Input.Get<object>(${quote(i.name)})`, details.input, "function");
    if (language === "python") add(`input.Get(${quote(i.name)})`, details.input, "function");
  }
  const outputAccessor = language === "javascript" ? "getOutputFrom" : language === "csharp" ? "Output.From" : language === "python" ? "output.Get" : undefined;
  if (outputAccessor) for (const activity of activities) {
    const detail = t("output of {{name}}", { name: activity.name });
    for (const output of activity.outputs) add(`${outputAccessor}(${quote(activity.name)}, ${quote(output)})`, detail, "function");
  }
  return items;
}
