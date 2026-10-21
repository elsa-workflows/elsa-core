"use client";

import { ChevronRight, Loader2 } from "lucide-react";
import { useMemo } from "react";
import { EmptyState } from "@/components/common/empty-state";
import { StatusBadge } from "@/components/common/status-badge";
import { formatTimestamp } from "@/components/common/timestamp";
import { Badge } from "@/components/ui/badge";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import type { ActivityExecutionRecord } from "@/lib/api/types/workflow-instances";
import { cn } from "@/lib/utils";
import { elapsedMs, executionDisplayText, formatElapsed, groupBy } from "../model";

/** Activity execution records grouped per activity, in first-execution order (the "call stack" view). */
export function ActivityExecutionsList({
  records,
  isLoading,
  selectedActivityId,
  onSelect,
}: {
  records: ActivityExecutionRecord[];
  isLoading: boolean;
  selectedActivityId?: string;
  onSelect: (record: ActivityExecutionRecord) => void;
}) {
  const groups = useMemo(() => [...groupBy(records, (r) => r.activityNodeId).entries()], [records]);

  if (isLoading)
    return (
      <div className="flex items-center gap-2 p-4 text-sm text-muted-foreground">
        <Loader2 className="size-4 animate-spin" /> Loading executions…
      </div>
    );
  if (groups.length === 0) return <EmptyState title="No activity executions" className="m-3 p-6" />;

  return (
    <div className="flex flex-col py-1">
      {groups.map(([nodeId, items]) => {
        const first = items[0];
        const selected = selectedActivityId === first.activityId;
        const depth = Math.max(0, nodeId.split(":").length - 2);
        return (
          <Collapsible key={nodeId} defaultOpen={selected}>
            <CollapsibleTrigger
              className={cn(
                "group flex w-full items-center gap-1.5 px-3 py-1.5 text-left text-sm hover:bg-muted/60",
                selected && "bg-primary/5 ring-1 ring-inset ring-primary/30",
              )}
              style={{ paddingLeft: `${0.75 + depth * 0.75}rem` }}
            >
              <ChevronRight className="size-3.5 shrink-0 text-muted-foreground transition-transform group-data-panel-open:rotate-90" />
              <span className="min-w-0 flex-1 truncate font-medium">{executionDisplayText(first)}</span>
              <Badge variant="secondary" className="h-4 px-1.5 text-[10px] tabular-nums">
                {items.length}
              </Badge>
              <StatusBadge status={items[items.length - 1].status} className="h-4 px-1.5 text-[10px]" />
            </CollapsibleTrigger>
            <CollapsibleContent>
              <ul className="flex flex-col pb-1" style={{ paddingLeft: `${2 + depth * 0.75}rem` }}>
                {items.map((r) => (
                  <li key={r.id}>
                    <button
                      type="button"
                      onClick={() => onSelect(r)}
                      className="flex w-full items-center justify-between gap-2 rounded px-2 py-1 text-left text-xs hover:bg-muted/60"
                    >
                      <span className="tabular-nums text-muted-foreground">{formatTimestamp(r.startedAt, "HH:mm:ss.SSS")}</span>
                      <span className="tabular-nums">{r.completedAt ? formatElapsed(elapsedMs(r.startedAt, r.completedAt)) : "running"}</span>
                      <StatusBadge status={r.status} className="h-4 px-1.5 text-[10px]" />
                    </button>
                  </li>
                ))}
              </ul>
            </CollapsibleContent>
          </Collapsible>
        );
      })}
    </div>
  );
}
