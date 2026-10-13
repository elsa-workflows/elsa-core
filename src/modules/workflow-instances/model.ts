import { differenceInMilliseconds, isValid, parseISO } from "date-fns";
import type { Activity } from "@/lib/api/types/activities";
import type { ActivityExecutionRecord, ActivityExecutionStats, WorkflowExecutionLogRecord, WorkflowStatus, WorkflowSubStatus } from "@/lib/api/types/workflow-instances";
import { indexActivities } from "@/modules/designer";

export const WORKFLOW_STATUSES: WorkflowStatus[] = ["Running", "Finished"];
export const WORKFLOW_SUB_STATUSES: WorkflowSubStatus[] = ["Pending", "Executing", "Suspended", "Finished", "Faulted", "Cancelled", "Interrupted"];

/** Container activities that wrap the user's own activities; hidden from the journal by default. */
export const SYSTEM_ACTIVITY_TYPES = new Set(["Elsa.Workflow", "Elsa.Flowchart", "Elsa.Sequence", "Elsa.StateMachine", "Elsa.WorkflowDefinitionActivity"]);

export function isSystemActivityType(type: string) {
  return SYSTEM_ACTIVITY_TYPES.has(type);
}

export type EventTone = "info" | "success" | "danger" | "warning" | "neutral";

export function toneForEvent(eventName: string): EventTone {
  switch (eventName) {
    case "Started":
      return "info";
    case "Completed":
      return "success";
    case "Faulted":
      return "danger";
    case "Suspended":
    case "Resumed":
      return "warning";
    default:
      return "neutral";
  }
}

export function journalDisplayText(record: WorkflowExecutionLogRecord) {
  return record.activityName?.trim() || record.activityType.split(".").pop() || record.activityType;
}

export function executionDisplayText(record: ActivityExecutionRecord) {
  return record.activityName?.trim() || record.activityType.split(".").pop() || record.activityType;
}

/** Milliseconds between two ISO timestamps, or `undefined` when either cannot be parsed. */
export function elapsedMs(from: string | undefined, to: string | undefined) {
  if (!from || !to) return undefined;
  const a = parseISO(from);
  const b = parseISO(to);
  if (!isValid(a) || !isValid(b)) return undefined;
  return differenceInMilliseconds(b, a);
}

export function formatElapsed(ms: number | undefined) {
  if (ms === undefined) return "";
  if (ms < 1000) return `${ms} ms`;
  if (ms < 60_000) return `${(ms / 1000).toFixed(ms < 10_000 ? 2 : 1)} s`;
  const minutes = Math.floor(ms / 60_000);
  const seconds = Math.round((ms % 60_000) / 1000);
  if (minutes < 60) return `${minutes}m ${seconds}s`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ${minutes % 60}m`;
}

/** Derives per-node statistics from execution records (fallback for servers without the report endpoint). */
export function statsFromRecords(records: ActivityExecutionRecord[]): ActivityExecutionStats[] {
  const map = new Map<string, ActivityExecutionStats>();
  for (const r of records) {
    const s = map.get(r.activityNodeId) ?? {
      activityNodeId: r.activityNodeId,
      startedCount: 0,
      completedCount: 0,
      uncompletedCount: 0,
      isBlocked: false,
      isFaulted: false,
    };
    s.startedCount += 1;
    if (r.status === "Completed") s.completedCount += 1;
    else s.uncompletedCount += 1;
    if (r.status === "Faulted") s.isFaulted = true;
    if (r.hasBookmarks && !r.completedAt) s.isBlocked = true;
    map.set(r.activityNodeId, s);
  }
  return [...map.values()];
}

/** Every activity in the tree with its node id (what execution records and stats are keyed by). */
export function activityNodeIds(root: Activity): string[] {
  const ids: string[] = [];
  for (const { activity } of indexActivities(root).values()) ids.push(activity.nodeId ?? activity.id);
  return ids;
}

export function groupBy<T, K>(items: T[], key: (item: T) => K): Map<K, T[]> {
  const map = new Map<K, T[]>();
  for (const item of items) {
    const k = key(item);
    const list = map.get(k);
    if (list) list.push(item);
    else map.set(k, [item]);
  }
  return map;
}
