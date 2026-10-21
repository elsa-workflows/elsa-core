import { expressionCompletions } from "@elsa-workflows/react-studio-sdk/designer/model/data-flow";
import type { UiHintContext } from "./types";
import type { CodeCompletion } from "@elsa-workflows/react-studio-ui/common/code-editor";
import type { CodeLanguage } from "@elsa-workflows/react-studio-sdk";

interface Member { name: string; type: string; method: boolean }
interface ServerTypes { types: Map<string, Member[]>; functions: CodeCompletion[]; globals: { name: string; type: string }[] }

/** Parses the line-oriented declaration format emitted by TypeDefinitionDocumentRenderer.
 * Unknown TypeScript syntax is ignored; this never executes server-provided code. */
export function parseServerTypes(document: string): ServerTypes {
  if (document.length > 2_000_000) throw new Error("Server type information exceeds 2 MB.");
  const result: ServerTypes = { types: new Map(), functions: [], globals: [] };
  let members: Member[] | undefined;
  for (const line of document.split(/\r?\n/)) {
    const declaration = line.match(/^declare (?:class|interface|enum) ([\w$]+(?:`\d+)?)\s*\{/);
    if (declaration) { members = []; result.types.set(declaration[1], members); continue; }
    if (line.trim() === "}") { members = undefined; continue; }
    const fn = line.match(/^declare function ([\w$]+)\((.*)\)(?:: (.+))?;/);
    if (fn) { result.functions.push({ label: fn[1], detail: line.replace(/^declare /, ""), type: "function" }); continue; }
    const global = line.match(/^declare var ([\w$]+): (.+);$/);
    if (global) { result.globals.push({ name: global[1], type: global[2] }); continue; }
    const member = line.trim().match(/^([\w$]+)\??:\s*(.+);$/);
    if (members && member) members.push({ name: member[1], type: member[2], method: member[2].startsWith("(") });
  }
  return result;
}

const pascal = (name: string) => name.replace(/(?:^|_)([a-z])/g, (_, letter: string) => letter.toUpperCase());
const identifier = /^[A-Za-z_][\w]*$/;
const generatedAccessor = /^[A-Za-z][A-Za-z0-9]*$/;

// CommonFunctionsDefinitionProvider's get/set functions are independent of saved
// workflow members. Unknown get*/set* declarations cannot be distinguished from
// stale generated wrappers in the server's untagged document, so omit those.
const commonAccessors = new Set([
  "getWorkflowDefinitionId", "getWorkflowDefinitionVersionId", "getWorkflowDefinitionVersion",
  "getWorkflowInstanceId", "getCorrelationId", "setCorrelationId", "getWorkflowInstanceName",
  "setWorkflowInstanceName", "setVariable", "getVariable", "getInput", "getOutputFrom",
  "getLastResult", "getShortGuid", "getGuid", "getGuidString",
]);

/** One-level typed members keep completion construction linear in workflow size. */
export function typedCompletions(context: Pick<UiHintContext, "variables" | "inputs" | "activities"> | undefined, language: CodeLanguage, server: ServerTypes): CodeCompletion[] {
  if (!context) return [];
  const completionLanguage = language === "typescript" ? "javascript" : language === "json" || language === "sql" ? "plaintext" : language;
  const local = expressionCompletions(context.variables, context.inputs ?? [], context.activities ?? [], completionLanguage);
  const items = new Map<string, CodeCompletion>(local.map(item => [item.label, item]));
  if (!["javascript", "typescript", "csharp", "python"].includes(language)) return local;
  const add = (label: string, detail: string, type = "variable") => items.set(label, { label, detail, type });
  const root = (label: string, type: string) => {
    add(label, type);
    const name = type.match(/^[\w.$`]+/)?.[0].split(".").at(-1) ?? type;
    for (const member of server.types.get(type) ?? server.types.get(name) ?? []) add(`${label}.${member.name}`, member.type, member.method ? "function" : "property");
  };
  const js = language === "javascript" || language === "typescript";
  const currentAccessors = new Set(commonAccessors);
  for (const variable of context.variables) {
    if (language === "csharp" ? generatedAccessor.test(variable.name) : identifier.test(variable.name)) root(`${language === "csharp" ? "Variable." + pascal(variable.name) : "variables." + variable.name}`, variable.typeName);
    if (js && generatedAccessor.test(variable.name)) {
      const name = pascal(variable.name);
      root(`get${name}()`, variable.typeName);
      currentAccessors.add(`get${name}`);
      currentAccessors.add(`set${name}`);
    }
  }
  for (const input of context.inputs ?? []) {
    if (js && generatedAccessor.test(input.name)) {
      root(`get${input.name}()`, input.type);
      currentAccessors.add(`get${input.name}`);
    }
    if (language === "csharp" && generatedAccessor.test(input.name)) root(`Input.${input.name}`, input.type);
    if (language === "python") root(`input.Get(${JSON.stringify(input.name)})`, input.type);
  }
  if (js) {
    for (const activity of context.activities ?? []) for (const output of activity.outputs) {
      if (identifier.test(activity.name) && identifier.test(output)) currentAccessors.add(`get${output}From${pascal(activity.name)}`);
    }
    for (const fn of server.functions) {
      if (!/^(?:get|set)/.test(fn.label) || currentAccessors.has(fn.label)) items.set(fn.label, fn);
    }
    for (const global of server.globals) { if (global.name === "variables") add(global.name, global.type); else root(global.name, global.type); }
  }
  return [...items.values()];
}
