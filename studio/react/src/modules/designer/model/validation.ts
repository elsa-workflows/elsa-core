import type { Activity, Flowchart, StateMachine } from "@/lib/api/types/activities";
import { isFlowchart, isStateMachine } from "@/lib/api/types/activities";
import { childActivities } from "./containers";
import type { ActivityRegistry } from "./registry";
import { computePorts } from "./ports";
import { activityDisplayText } from "./display";

export type IssueSeverity = "warning" | "info";

export interface DesignerIssue {
  id: string;
  severity: IssueSeverity;
  message: string;
  /** Activity the issue points at (so the UI can reveal it). */
  activityId?: string;
  /** State machine findings point at a state or a transition instead. */
  stateName?: string;
  transitionIndex?: number;
}

/**
 * Client-side checks mirroring what elsa-core enforces or silently tolerates: disconnected flowchart
 * steps, dangling connections, state machines whose transitions reference unknown states, shared
 * trigger activities (rejected by the server), missing initial state. The server stays authoritative.
 */
export function validateWorkflow(root: Activity, registry: ActivityRegistry): DesignerIssue[] {
  const issues: DesignerIssue[] = [];
  const visit = (activity: Activity, depth: number) => {
    if (isFlowchart(activity)) validateFlowchartContainer(activity, registry, depth, issues);
    if (isStateMachine(activity)) validateStateMachine(activity, registry, issues);
    for (const child of childActivities(activity)) visit(child, depth + 1);
  };
  visit(root, 0);

  if (isFlowchart(root)) {
    const starters = root.activities.filter((a) => {
      const d = registry.get(a.type, a.version);
      return d?.kind === "Trigger" || d?.isStart || a.customProperties?.canStartWorkflow === true;
    });
    if (root.activities.length > 0 && starters.length === 0 && !root.start) {
      issues.push({ id: "no-trigger", severity: "info", message: "No trigger or start activity: the workflow can only be started manually (Run) and begins at the first activity." });
    }
  }
  return issues;
}

/** @deprecated use validateWorkflow */
export const validateFlowchart = validateWorkflow;

function validateFlowchartContainer(container: Flowchart, registry: ActivityRegistry, depth: number, issues: DesignerIssue[]) {
  const ids = new Set(container.activities.map((a) => a.id));
  const incoming = new Map<string, number>();
  const outgoing = new Map<string, number>();
  for (const c of container.connections) {
    if (!ids.has(c.source.activity) || !ids.has(c.target.activity)) {
      issues.push({
        id: `dangling:${c.source.activity}:${c.source.port}:${c.target.activity}`,
        severity: "warning",
        message: "A connection points at an activity that no longer exists.",
        activityId: ids.has(c.source.activity) ? c.source.activity : ids.has(c.target.activity) ? c.target.activity : undefined,
      });
      continue;
    }
    incoming.set(c.target.activity, (incoming.get(c.target.activity) ?? 0) + 1);
    outgoing.set(c.source.activity, (outgoing.get(c.source.activity) ?? 0) + 1);
  }
  const targets = new Set(container.connections.map((c) => c.target.activity));
  const roots = container.activities.filter((a) => !targets.has(a.id));
  for (const a of container.activities) {
    const descriptor = registry.get(a.type, a.version);
    const name = activityDisplayText(a, descriptor);
    void computePorts(a, descriptor);
    const isEntry = descriptor?.kind === "Trigger" || descriptor?.isStart || a.customProperties?.canStartWorkflow === true || container.start === a.id;
    const hasIn = (incoming.get(a.id) ?? 0) > 0;
    const hasOut = (outgoing.get(a.id) ?? 0) > 0;
    if (container.activities.length > 1 && !hasIn && !hasOut) {
      issues.push({ id: `isolated:${a.id}`, severity: "warning", message: `"${name}" is not connected to anything.`, activityId: a.id });
    } else if (!isEntry && !hasIn && container.activities.length > 1 && !(depth === 0 && roots.length === 1 && roots[0].id === a.id)) {
      issues.push({ id: `no-in:${a.id}`, severity: "info", message: `"${name}" has no incoming connection and will only run if it is the first activity.`, activityId: a.id });
    }
  }
}

function validateStateMachine(sm: StateMachine, registry: ActivityRegistry, issues: DesignerIssue[]) {
  const names = sm.states.map((s) => s.name);
  const nameSet = new Set(names);
  const smId = sm.id;

  sm.states.forEach((s, i) => {
    if (!s.name.trim()) issues.push({ id: `sm:${smId}:empty-name:${i}`, severity: "warning", message: "A state has no name.", stateName: s.name });
  });
  const dupes = names.filter((n, i) => n && names.indexOf(n) !== i);
  for (const d of new Set(dupes)) issues.push({ id: `sm:${smId}:dup:${d}`, severity: "warning", message: `State name "${d}" is used more than once; the server resolves states by name.`, stateName: d });

  if (sm.states.length > 0 && (!sm.initialState || !nameSet.has(sm.initialState))) {
    issues.push({ id: `sm:${smId}:initial`, severity: "warning", message: sm.initialState ? `Initial state "${sm.initialState}" does not exist; the state machine completes immediately.` : "No initial state: the state machine completes immediately.", activityId: smId });
  }

  const triggerIds = new Map<string, number>();
  sm.transitions.forEach((t, i) => {
    const label = t.displayName || t.name || `${t.from} → ${t.to}`;
    if (!nameSet.has(t.from)) issues.push({ id: `sm:${smId}:from:${i}`, severity: "warning", message: `Transition "${label}" starts from unknown state "${t.from}".`, transitionIndex: i });
    if (!nameSet.has(t.to)) issues.push({ id: `sm:${smId}:to:${i}`, severity: "warning", message: `Transition "${label}" targets unknown state "${t.to}" and is ignored by the server.`, transitionIndex: i });
    if (t.trigger) {
      const prev = triggerIds.get(t.trigger.id);
      if (prev !== undefined) issues.push({ id: `sm:${smId}:shared-trigger:${i}`, severity: "warning", message: `Transition "${label}" shares its trigger with another transition; the server rejects shared triggers.`, transitionIndex: i });
      triggerIds.set(t.trigger.id, i);
      const d = registry.get(t.trigger.type, t.trigger.version);
      if (d && d.kind !== "Trigger") issues.push({ id: `sm:${smId}:trigger-kind:${i}`, severity: "info", message: `Transition "${label}" uses "${d.displayName}" as trigger; it completes immediately, so the transition fires as soon as the state is entered.`, transitionIndex: i });
    }
  });

  // Reachability from the initial state.
  if (sm.initialState && nameSet.has(sm.initialState)) {
    const reachable = new Set<string>([sm.initialState]);
    const stack = [sm.initialState];
    while (stack.length) {
      const cur = stack.pop()!;
      for (const t of sm.transitions) if (t.from === cur && nameSet.has(t.to) && !reachable.has(t.to)) { reachable.add(t.to); stack.push(t.to); }
    }
    for (const n of names) if (n && !reachable.has(n)) issues.push({ id: `sm:${smId}:unreachable:${n}`, severity: "info", message: `State "${n}" cannot be reached from the initial state.`, stateName: n });
  }
}

export function issuesByActivity(issues: DesignerIssue[]): Map<string, DesignerIssue[]> {
  const map = new Map<string, DesignerIssue[]>();
  for (const i of issues) {
    if (!i.activityId) continue;
    map.set(i.activityId, [...(map.get(i.activityId) ?? []), i]);
  }
  return map;
}
