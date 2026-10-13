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

/* ------------------------------------------------------------------------------------------------
 * Journal spans
 *
 * The journal arrives as a flat event log: every activity emits Started, then some combination of
 * Suspended / Resumed, then Completed or Faulted. Rendered one row per event it is mostly repetition
 * -- three activities produced eight near-identical rows in the old list.
 *
 * These helpers fold the events of one activity execution into a single span (status + duration) and
 * rebuild the nesting the workflow actually had. Hierarchy comes from `nodeId`, which is a
 * colon-delimited path (`Workflow1:flowchart1:reviewOrder`); `parentActivityInstanceId` is used when
 * the server sends it, which not every version does.
 * ---------------------------------------------------------------------------------------------- */

export interface JournalException {
  type?: string;
  message?: string;
  source?: string;
  stackTrace?: string;
}

/** The exception a faulted journal record carries; the server puts it in `payload`, not in a typed field. */
export function journalException(record: WorkflowExecutionLogRecord): JournalException | undefined {
  const payload = record.payload as Record<string, unknown> | undefined;
  if (!payload || typeof payload.exception !== "string") return undefined;
  const read = (key: string) => (typeof payload[key] === "string" ? (payload[key] as string) : undefined);
  return { type: payload.exception, message: read("message"), source: read("source"), stackTrace: read("stackTrace") };
}

export type JournalSpanStatus = "running" | "completed" | "faulted" | "suspended" | "cancelled";

export interface JournalSpan {
  /** Stable across re-renders: the activity instance, or the node path when the server omits it. */
  key: string;
  activityId: string;
  activityInstanceId?: string;
  nodeId: string;
  name: string;
  activityType: string;
  /** A container the engine added (workflow, flowchart, sequence) rather than an activity the author placed. */
  isContainer: boolean;
  status: JournalSpanStatus;
  startedAt?: string;
  endedAt?: string;
  /** Wall-clock time from start to terminal event: execution plus any time spent waiting. */
  durationMs?: number;
  /**
   * Time spent suspended, summed over every Suspended → Resumed pair that closed, plus whatever its
   * children waited for. Undefined when nothing ever suspended.
   */
  suspendedMs?: number;
  /** Execution time: {@link durationMs} with {@link suspendedMs} taken out. This is the one to rank by. */
  activeMs?: number;
  /** Every event folded into this span, in order. */
  events: WorkflowExecutionLogRecord[];
  /** The event a click should select; the terminal one carries the exception when there is one. */
  record: WorkflowExecutionLogRecord;
  hasException: boolean;
  children: JournalSpan[];
}

function statusOf(events: WorkflowExecutionLogRecord[]): JournalSpanStatus {
  const names = events.map((e) => e.eventName);
  if (names.includes("Faulted")) return "faulted";
  if (names.includes("Cancelled")) return "cancelled";
  if (names.includes("Completed")) return "completed";
  if (names.lastIndexOf("Suspended") > names.lastIndexOf("Resumed")) return "suspended";
  return "running";
}

const TERMINAL_EVENTS = new Set(["Completed", "Faulted", "Cancelled"]);

/**
 * Time a span sat suspended, over every Suspended → Resumed pair it closed.
 *
 * A user task that waits two hours for a human is not slow, but its wall-clock duration says it is.
 * Separating the two is what makes the duration column mean "how long this took to run". A suspension
 * that has not resumed yet is not counted: the span has no end either, so there is nothing to subtract from.
 */
function suspendedMsOf(events: WorkflowExecutionLogRecord[]): number | undefined {
  let total: number | undefined;
  let openedAt: string | undefined;

  for (const event of events) {
    if (event.eventName === "Suspended") openedAt = event.timestamp;
    else if (event.eventName === "Resumed" && openedAt) {
      total = (total ?? 0) + (elapsedMs(openedAt, event.timestamp) ?? 0);
      openedAt = undefined;
    }
  }

  return total;
}

/** Groups the journal into one span per activity execution, then nests them by node path. */
export function buildJournalSpans(records: WorkflowExecutionLogRecord[]): JournalSpan[] {
  const ordered = [...records].sort((a, b) => a.sequence - b.sequence);
  const byKey = new Map<string, WorkflowExecutionLogRecord[]>();

  for (const record of ordered) {
    // Without an activity instance id the node path is the best available identity; a looping
    // activity then folds into one span, which is still better than one row per event.
    const key = record.activityInstanceId || record.nodeId;
    const group = byKey.get(key);
    if (group) group.push(record);
    else byKey.set(key, [record]);
  }

  const spans = [...byKey.entries()].map(([key, events]) => {
    const first = events[0];
    const terminal = [...events].reverse().find((e) => TERMINAL_EVENTS.has(e.eventName));
    const started = events.find((e) => e.eventName === "Started") ?? first;
    const span: JournalSpan = {
      key,
      activityId: first.activityId,
      activityInstanceId: first.activityInstanceId,
      nodeId: first.nodeId,
      name: journalDisplayText(first),
      activityType: first.activityType,
      isContainer: isSystemActivityType(first.activityType),
      status: statusOf(events),
      startedAt: started.timestamp,
      endedAt: terminal?.timestamp,
      durationMs: elapsedMs(started.timestamp, terminal?.timestamp),
      suspendedMs: suspendedMsOf(events),
      events,
      record: terminal ?? first,
      hasException: events.some((e) => !!journalException(e)),
      children: [],
    };
    return span;
  });

  const roots = nestSpans(spans);
  for (const root of roots) resolveWaitTime(root);
  return roots;
}

/**
 * Pushes wait time up the tree: a flowchart whose only activity waited an hour did not work for an
 * hour. Children's waits are summed, which overstates it when branches suspend in parallel, so the
 * result is clamped to the span's own duration.
 */
function resolveWaitTime(span: JournalSpan): number | undefined {
  const fromChildren = span.children.map(resolveWaitTime).filter((ms): ms is number => ms !== undefined);
  const total = span.suspendedMs !== undefined || fromChildren.length > 0 ? (span.suspendedMs ?? 0) + fromChildren.reduce((a, b) => a + b, 0) : undefined;

  span.suspendedMs = total === undefined ? undefined : span.durationMs === undefined ? total : Math.min(total, span.durationMs);
  span.activeMs = span.durationMs === undefined ? undefined : Math.max(0, span.durationMs - (span.suspendedMs ?? 0));
  return span.suspendedMs;
}

function parentNodeId(nodeId: string) {
  const at = nodeId.lastIndexOf(":");
  return at === -1 ? undefined : nodeId.slice(0, at);
}

function nestSpans(spans: JournalSpan[]): JournalSpan[] {
  const byInstanceId = new Map(spans.filter((s) => s.activityInstanceId).map((s) => [s.activityInstanceId!, s]));
  const byNodeId = new Map<string, JournalSpan[]>();
  for (const span of spans) {
    const siblings = byNodeId.get(span.nodeId);
    if (siblings) siblings.push(span);
    else byNodeId.set(span.nodeId, [span]);
  }

  const roots: JournalSpan[] = [];

  for (const span of spans) {
    const parent = findParent(span, spans, byInstanceId, byNodeId);
    if (parent && parent !== span) parent.children.push(span);
    else roots.push(span);
  }

  return roots;
}

function findParent(
  span: JournalSpan,
  spans: JournalSpan[],
  byInstanceId: Map<string, JournalSpan>,
  byNodeId: Map<string, JournalSpan[]>,
): JournalSpan | undefined {
  const declared = span.events[0].parentActivityInstanceId;
  if (declared) return byInstanceId.get(declared);

  const parentNode = parentNodeId(span.nodeId);
  if (!parentNode) return undefined;

  const candidates = byNodeId.get(parentNode);
  if (!candidates?.length) return undefined;
  if (candidates.length === 1) return candidates[0];

  // A node that runs more than once (a loop body) has several candidate parents: the right one is the
  // most recent iteration that had already started when this span did.
  const startedAt = span.startedAt ?? "";
  return [...candidates].reverse().find((c) => (c.startedAt ?? "") <= startedAt) ?? candidates[0];
}

/** Drops container spans, promoting their children in place -- the flat view the journal had before. */
export function flattenContainers(spans: JournalSpan[]): JournalSpan[] {
  return spans.flatMap((span) => (span.isContainer ? flattenContainers(span.children) : [{ ...span, children: flattenContainers(span.children) }]));
}

/** Depth-first walk of the spans a viewer should render, honouring which subtrees are collapsed. */
export function visibleSpans(
  spans: JournalSpan[],
  isCollapsed: (span: JournalSpan, depth: number) => boolean,
  depth = 0,
): { span: JournalSpan; depth: number }[] {
  return spans.flatMap((span) => [
    { span, depth },
    ...(isCollapsed(span, depth) ? [] : visibleSpans(span.children, isCollapsed, depth + 1)),
  ]);
}

export function countSpans(spans: JournalSpan[]): number {
  return spans.reduce((total, span) => total + 1 + countSpans(span.children), 0);
}
