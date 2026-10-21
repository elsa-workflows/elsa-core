import type { Activity } from "@/lib/api/types/activities";
import type { WorkflowDefinition, WorkflowDefinitionSummary } from "@/lib/api/types/workflow-definitions";
import type { CallGraphApi } from "../call-graph-service";
import type { CallGraph, CallGraphNode } from "../types";

let seq = 0;
const id = () => `a${++seq}`;

export const literal = (value: string) => ({ typeName: "String", expression: { type: "Literal", value } });
export const expr = (code: string) => ({ typeName: "String", expression: { type: "JavaScript", value: code } });

export function dispatch(target: string | ReturnType<typeof expr>, activityId = id()): Activity {
  return { id: activityId, type: "Elsa.DispatchWorkflow", version: 1, workflowDefinitionId: typeof target === "string" ? literal(target) : target };
}

export function execute(target: string | ReturnType<typeof expr>, activityId = id()): Activity {
  return { id: activityId, type: "Elsa.ExecuteWorkflow", version: 1, workflowDefinitionId: typeof target === "string" ? literal(target) : target };
}

export function writeLine(activityId = id()): Activity {
  return { id: activityId, type: "Elsa.WriteLine", version: 1, text: literal("hi") };
}

export function flowchart(activities: Activity[], activityId = id()): Activity {
  return { id: activityId, type: "Elsa.Flowchart", version: 1, activities, connections: [] };
}

export function sequence(activities: Activity[], activityId = id()): Activity {
  return { id: activityId, type: "Elsa.Sequence", version: 1, activities };
}

/** An `If` with embedded `then` / `else` children (no descriptor: the generic child discovery applies). */
export function ifActivity(then: Activity | undefined, otherwise?: Activity, activityId = id()): Activity {
  return { id: activityId, type: "Elsa.If", version: 1, condition: literal("true"), then, else: otherwise };
}

export function forEach(body: Activity, activityId = id()): Activity {
  return { id: activityId, type: "Elsa.ForEach", version: 1, body };
}

export function definition(definitionId: string, root: Activity, extra: Partial<WorkflowDefinition> = {}): WorkflowDefinition {
  return {
    id: `${definitionId}-v1`,
    definitionId,
    name: definitionId.toUpperCase(),
    version: 1,
    isLatest: true,
    isPublished: true,
    createdAt: "2026-01-01T00:00:00Z",
    variables: [],
    inputs: [],
    outputs: [],
    outcomes: [],
    customProperties: {},
    options: {},
    root,
    ...extra,
  };
}

export function summaryOf(d: WorkflowDefinition): WorkflowDefinitionSummary {
  return { id: d.id, definitionId: d.definitionId, name: d.name, version: d.version, isLatest: d.isLatest, isPublished: d.isPublished, createdAt: d.createdAt };
}

/** In-memory API: `listed` are returned by the list; `unlisted` only by `getByDefinitionId`. */
export function fakeApi(listed: WorkflowDefinition[], unlisted: WorkflowDefinition[] = [], options: { failMany?: boolean; delayMs?: number } = {}) {
  const calls = { list: 0, getMany: 0, getByDefinitionId: 0, getByDefinitionIdIds: [] as string[] };
  const wait = () => (options.delayMs ? new Promise((r) => setTimeout(r, options.delayMs)) : Promise.resolve());
  const api: CallGraphApi = {
    async list(args) {
      calls.list++;
      await wait();
      const start = args.page * args.pageSize;
      return { items: listed.slice(start, start + args.pageSize).map(summaryOf), totalCount: listed.length };
    },
    async getMany(ids) {
      calls.getMany++;
      await wait();
      if (options.failMany) throw new Error("bulk failed");
      return { items: listed.filter((d) => ids.includes(d.id)) };
    },
    async getByDefinitionId(definitionId) {
      calls.getByDefinitionId++;
      calls.getByDefinitionIdIds.push(definitionId);
      await wait();
      const found = [...listed, ...unlisted].find((d) => d.definitionId === definitionId);
      if (!found) throw new Error("404 not found");
      return found;
    },
  };
  return { api, calls };
}

/** Builds a CallGraph directly from (caller -> targets) pairs for subgraph tests. */
export function graphOf(edges: Record<string, (string | undefined)[]>, missing: string[] = []): CallGraph {
  const nodes = new Map<string, CallGraphNode>();
  const ensure = (definitionId: string) => {
    let n = nodes.get(definitionId);
    if (!n) {
      n = { definitionId, name: definitionId.toUpperCase(), status: "loaded", storedEdges: [] };
      nodes.set(definitionId, n);
    }
    return n;
  };
  for (const [caller, targets] of Object.entries(edges)) {
    const node = ensure(caller);
    targets.forEach((target, i) => {
      node.storedEdges.push({ callerId: caller, targetId: target, callType: i % 2 ? "Execute" : "Dispatch", activityId: `${caller}-call-${i}`, activityType: "Elsa.DispatchWorkflow", expressionType: target ? undefined : "JavaScript" });
      if (target) ensure(target);
    });
  }
  for (const m of missing) {
    const n = ensure(m);
    n.status = "missing";
  }
  return { nodes, status: "ready", warnings: [], complete: true };
}
