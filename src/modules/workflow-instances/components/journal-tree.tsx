"use client";

import { AlertTriangle, Ban, CheckCircle2, ChevronRight, Circle, ChevronsDownUp, ChevronsUpDown, Loader2, PauseCircle, type LucideIcon } from "lucide-react";
import { useCallback, useMemo, useRef, useState } from "react";
import { EmptyState } from "@/components/common/empty-state";
import { formatTimestamp } from "@/components/common/timestamp";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import type { WorkflowExecutionLogRecord } from "@/lib/api/types/workflow-instances";
import { cn } from "@/lib/utils";
import {
  buildJournalSpans,
  countSpans,
  elapsedMs,
  flattenContainers,
  formatElapsed,
  journalException,
  visibleSpans,
  type JournalSpan,
  type JournalSpanStatus,
} from "../model";

/**
 * The journal as a span tree rather than an event log.
 *
 * The API returns one record per lifecycle event, so a three-activity workflow arrives as eight rows
 * that mostly repeat each other. Each row here is one activity execution -- its status and how long it
 * took -- and the events that produced it are revealed on click. Nesting follows the workflow's own
 * structure, so a flowchart owns the activities inside it.
 *
 * Interaction follows the conventions of trace viewers: a chevron folds a subtree, the row body
 * selects and reveals detail, arrow keys walk the tree, and containers are hidden by default.
 */

/** Subtrees deeper than this start folded, so a large workflow opens readable rather than complete. */
const DEFAULT_EXPANDED_DEPTH = 2;

const STATUS: Record<JournalSpanStatus, { icon: LucideIcon; className: string; label: string }> = {
  completed: { icon: CheckCircle2, className: "text-emerald-600 dark:text-emerald-400", label: "Completed" },
  faulted: { icon: AlertTriangle, className: "text-red-600 dark:text-red-400", label: "Faulted" },
  suspended: { icon: PauseCircle, className: "text-amber-600 dark:text-amber-400", label: "Suspended" },
  cancelled: { icon: Ban, className: "text-muted-foreground", label: "Cancelled" },
  running: { icon: Circle, className: "text-sky-600 dark:text-sky-400", label: "Running" },
};

const EVENT_TONE: Record<string, string> = {
  Started: "bg-sky-500",
  Completed: "bg-emerald-500",
  Faulted: "bg-red-500",
  Suspended: "bg-amber-500",
  Resumed: "bg-amber-500",
};

export function JournalTree({
  records,
  totalCount,
  selectedActivityId,
  onSelect,
  hasNextPage,
  isFetchingNextPage,
  isLoading,
  fetchNextPage,
}: {
  records: WorkflowExecutionLogRecord[];
  totalCount: number;
  selectedActivityId?: string;
  onSelect: (record: WorkflowExecutionLogRecord) => void;
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
  isLoading: boolean;
  fetchNextPage: () => void;
}) {
  const [hideContainers, setHideContainers] = useState(true);
  /** Only the subtrees the reader has actually toggled; everything else follows the depth default. */
  const [overrides, setOverrides] = useState<Record<string, boolean>>({});
  const [openDetail, setOpenDetail] = useState<string | null>(null);
  const listRef = useRef<HTMLOListElement>(null);

  const roots = useMemo(() => {
    const spans = buildJournalSpans(records);
    return hideContainers ? flattenContainers(spans) : spans;
  }, [records, hideContainers]);

  const isCollapsed = useCallback(
    (span: JournalSpan, depth: number) => overrides[span.key] ?? depth >= DEFAULT_EXPANDED_DEPTH,
    [overrides],
  );

  const rows = useMemo(() => visibleSpans(roots, isCollapsed), [roots, isCollapsed]);
  const spanCount = useMemo(() => countSpans(roots), [roots]);
  /** With containers hidden most workflows are a flat list, where expand-all controls do nothing. */
  const hasNesting = useMemo(() => roots.some((span) => span.children.length > 0), [roots]);
  const firstStart = roots[0]?.startedAt;

  const setCollapsed = (span: JournalSpan, collapsed: boolean) => setOverrides((o) => ({ ...o, [span.key]: collapsed }));

  const setAll = (collapsed: boolean) => {
    const next: Record<string, boolean> = {};
    const walk = (spans: JournalSpan[]) => {
      for (const span of spans) {
        if (span.children.length > 0) next[span.key] = collapsed;
        walk(span.children);
      }
    };
    walk(roots);
    setOverrides(next);
  };

  /** Arrow keys walk the tree the way a tree grid is expected to: a row at a time, subtree in and out. */
  const onKeyDown = (e: React.KeyboardEvent, span: JournalSpan, depth: number, index: number) => {
    const items = () => Array.from(listRef.current?.querySelectorAll<HTMLElement>('[role="treeitem"]') ?? []);
    const focus = (i: number) => items()[i]?.focus();
    const collapsed = isCollapsed(span, depth);

    if (e.key === "ArrowDown") {
      e.preventDefault();
      focus(index + 1);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      focus(index - 1);
    } else if (e.key === "ArrowRight") {
      e.preventDefault();
      if (span.children.length > 0 && collapsed) setCollapsed(span, false);
      else if (span.children.length > 0) focus(index + 1);
    } else if (e.key === "ArrowLeft") {
      e.preventDefault();
      if (span.children.length > 0 && !collapsed) setCollapsed(span, true);
      else {
        // Walk back to the nearest shallower row: the parent.
        const parent = rows.slice(0, index).reverse().find((r) => r.depth < depth);
        if (parent) focus(rows.indexOf(parent));
      }
    }
  };

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex items-center justify-between gap-2 border-b px-3 py-2 text-xs text-muted-foreground">
        <span>
          {spanCount} {spanCount === 1 ? "activity" : "activities"}
          {totalCount > records.length ? ` · ${records.length} of ${totalCount} events` : ""}
        </span>
        <div className="flex items-center gap-1">
          {hasNesting && (
            <>
              <Button variant="ghost" size="icon-sm" aria-label="Expand all" title="Expand all" onClick={() => setAll(false)}>
                <ChevronsUpDown />
              </Button>
              <Button variant="ghost" size="icon-sm" aria-label="Collapse all" title="Collapse all" onClick={() => setAll(true)}>
                <ChevronsDownUp />
              </Button>
            </>
          )}
          <Switch id="journal-hide-containers" size="sm" checked={hideContainers} onCheckedChange={setHideContainers} className="ml-1" />
          <Label htmlFor="journal-hide-containers" className="font-normal text-xs">
            Hide containers
          </Label>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto">
        {isLoading ? (
          <div className="flex items-center gap-2 p-4 text-sm text-muted-foreground">
            <Loader2 className="size-4 animate-spin" /> Loading journal…
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            title="No journal entries"
            description={hideContainers && records.length > 0 ? "Only the engine's own containers ran so far. Turn off “Hide containers” to see them." : undefined}
            className="m-3 p-6"
          />
        ) : (
          <ol ref={listRef} role="tree" aria-label="Workflow journal" className="flex flex-col py-1">
            {rows.map(({ span, depth }, index) => (
              <JournalRow
                key={span.key}
                span={span}
                depth={depth}
                index={index}
                collapsed={isCollapsed(span, depth)}
                selected={selectedActivityId === span.activityId}
                detailOpen={openDetail === span.key}
                offsetMs={elapsedMs(firstStart, span.startedAt)}
                onToggle={() => setCollapsed(span, !isCollapsed(span, depth))}
                onActivate={() => {
                  onSelect(span.record);
                  setOpenDetail((current) => (current === span.key ? null : span.key));
                }}
                onKeyDown={(e) => onKeyDown(e, span, depth, index)}
              />
            ))}
          </ol>
        )}
        {hasNextPage && (
          <div className="p-2">
            <Button variant="outline" size="sm" className="w-full" onClick={fetchNextPage} disabled={isFetchingNextPage}>
              {isFetchingNextPage ? <Loader2 className="animate-spin" /> : null}
              Load more
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}

function JournalRow({
  span,
  depth,
  index,
  collapsed,
  selected,
  detailOpen,
  offsetMs,
  onToggle,
  onActivate,
  onKeyDown,
}: {
  span: JournalSpan;
  depth: number;
  index: number;
  collapsed: boolean;
  selected: boolean;
  detailOpen: boolean;
  offsetMs?: number;
  onToggle: () => void;
  onActivate: () => void;
  onKeyDown: (e: React.KeyboardEvent) => void;
}) {
  const { icon: Icon, className, label } = STATUS[span.status];
  const hasChildren = span.children.length > 0;

  return (
    <li role="none">
      <div
        role="treeitem"
        tabIndex={index === 0 ? 0 : -1}
        aria-level={depth + 1}
        aria-selected={selected}
        aria-expanded={hasChildren ? !collapsed : undefined}
        onClick={onActivate}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onActivate();
          } else {
            onKeyDown(e);
          }
        }}
        className={cn(
          "flex cursor-pointer items-center gap-1.5 py-1 pr-3 text-sm outline-none hover:bg-muted/60 focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring/50",
          selected && "bg-primary/5 ring-1 ring-inset ring-primary/30",
        )}
        style={{ paddingLeft: `${0.5 + depth * 0.875}rem` }}
      >
        {hasChildren ? (
          <button
            type="button"
            tabIndex={-1}
            aria-label={collapsed ? `Expand ${span.name}` : `Collapse ${span.name}`}
            onClick={(e) => {
              e.stopPropagation();
              onToggle();
            }}
            className="-m-1 rounded p-1 text-muted-foreground hover:text-foreground"
          >
            <ChevronRight className={cn("size-3.5 transition-transform", !collapsed && "rotate-90")} />
          </button>
        ) : (
          <span className="size-3.5 shrink-0" aria-hidden />
        )}
        <Icon className={cn("size-4 shrink-0", className)} aria-hidden />
        <span className="min-w-0 flex-1 truncate font-medium" title={`${span.name} · ${span.activityType}`}>
          {span.name}
        </span>
        {hasChildren && collapsed && <span className="shrink-0 text-[11px] text-muted-foreground tabular-nums">{countDescendants(span)}</span>}
        <span className="shrink-0 text-xs text-muted-foreground tabular-nums">{span.durationMs === undefined ? label : formatElapsed(span.durationMs)}</span>
      </div>

      {detailOpen && (
        <div className="border-y bg-muted/30 px-3 py-2 text-xs" style={{ paddingLeft: `${1.75 + depth * 0.875}rem` }}>
          <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-0.5 text-muted-foreground">
            <dt>Type</dt>
            <dd className="truncate font-mono text-[11px] text-foreground" title={span.activityType}>
              {span.activityType}
            </dd>
            <dt>Status</dt>
            <dd className={className}>{label}</dd>
            {offsetMs !== undefined && offsetMs > 0 && (
              <>
                <dt>Offset</dt>
                <dd className="text-foreground tabular-nums">+{formatElapsed(offsetMs)}</dd>
              </>
            )}
          </dl>

          <ol className="mt-2 flex flex-col gap-1">
            {span.events.map((event) => {
              const since = elapsedMs(span.startedAt, event.timestamp);
              const exception = journalException(event);
              return (
                <li key={event.id} className="flex items-start gap-2">
                  <span className={cn("mt-1.5 size-1.5 shrink-0 rounded-full", EVENT_TONE[event.eventName] ?? "bg-muted-foreground")} aria-hidden />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-baseline justify-between gap-2">
                      <span className="font-medium text-foreground">{event.eventName}</span>
                      <span className="shrink-0 text-muted-foreground tabular-nums">
                        {formatTimestamp(event.timestamp, "HH:mm:ss.SSS")}
                        {since ? ` · +${formatElapsed(since)}` : ""}
                      </span>
                    </div>
                    {event.message && <p className="text-muted-foreground">{event.message}</p>}
                    {exception && (
                      <p className="mt-0.5 font-mono text-[11px] break-words text-red-600 dark:text-red-400">
                        {exception.type}
                        {exception.message && exception.message !== event.message ? `: ${exception.message}` : ""}
                      </p>
                    )}
                  </div>
                </li>
              );
            })}
          </ol>
        </div>
      )}
    </li>
  );
}

function countDescendants(span: JournalSpan): number {
  return span.children.reduce((total, child) => total + 1 + countDescendants(child), 0);
}
