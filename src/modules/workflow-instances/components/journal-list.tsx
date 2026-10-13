"use client";

import { AlertTriangle, CheckCircle2, Circle, Loader2, PauseCircle, Play, PlayCircle, type LucideIcon } from "lucide-react";
import { useMemo, useState } from "react";
import { EmptyState } from "@/components/common/empty-state";
import { formatTimestamp } from "@/components/common/timestamp";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import type { WorkflowExecutionLogRecord } from "@/lib/api/types/workflow-instances";
import { cn } from "@/lib/utils";
import { elapsedMs, formatElapsed, isSystemActivityType, journalDisplayText, toneForEvent, type EventTone } from "../model";

const toneStyles: Record<EventTone, { icon: LucideIcon; className: string }> = {
  info: { icon: Play, className: "text-sky-600 dark:text-sky-400" },
  success: { icon: CheckCircle2, className: "text-emerald-600 dark:text-emerald-400" },
  danger: { icon: AlertTriangle, className: "text-red-600 dark:text-red-400" },
  warning: { icon: PauseCircle, className: "text-amber-600 dark:text-amber-400" },
  neutral: { icon: Circle, className: "text-muted-foreground" },
};

function eventIcon(eventName: string) {
  const tone = toneForEvent(eventName);
  if (eventName === "Resumed") return { icon: PlayCircle, className: toneStyles.warning.className };
  return toneStyles[tone];
}

export function JournalList({
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
  const [hideSystem, setHideSystem] = useState(true);

  const visible = useMemo(() => {
    const filtered = hideSystem ? records.filter((r) => !isSystemActivityType(r.activityType)) : records;
    return filtered.map((r, i) => ({ record: r, elapsed: i > 0 ? elapsedMs(filtered[i - 1].timestamp, r.timestamp) : undefined }));
  }, [records, hideSystem]);

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex items-center justify-between gap-2 border-b px-3 py-2 text-xs text-muted-foreground">
        <span>
          {records.length}
          {totalCount > records.length ? ` of ${totalCount}` : ""} entries
        </span>
        <div className="flex items-center gap-1.5">
          <Switch id="journal-hide-system" size="sm" checked={hideSystem} onCheckedChange={(c) => setHideSystem(c)} />
          <Label htmlFor="journal-hide-system" className="font-normal text-xs">
            Hide system
          </Label>
        </div>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        {isLoading ? (
          <div className="flex items-center gap-2 p-4 text-sm text-muted-foreground">
            <Loader2 className="size-4 animate-spin" /> Loading journal…
          </div>
        ) : visible.length === 0 ? (
          <EmptyState title="No journal entries" description={hideSystem && records.length > 0 ? "Only system activities ran so far." : undefined} className="m-3 p-6" />
        ) : (
          <ol className="relative flex flex-col py-1">
            {visible.map(({ record, elapsed }) => {
              const { icon: Icon, className } = eventIcon(record.eventName);
              const selected = selectedActivityId === record.activityId;
              return (
                <li key={record.id}>
                  <button
                    type="button"
                    onClick={() => onSelect(record)}
                    className={cn(
                      "flex w-full items-start gap-2 px-3 py-1.5 text-left text-sm hover:bg-muted/60",
                      selected && "bg-primary/5 ring-1 ring-inset ring-primary/30",
                    )}
                  >
                    <Icon className={cn("mt-0.5 size-4 shrink-0", className)} />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center justify-between gap-2">
                        <span className="truncate font-medium">{journalDisplayText(record)}</span>
                        <span className={cn("shrink-0 text-xs", className)}>{record.eventName}</span>
                      </div>
                      <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground">
                        <span className="truncate">{record.activityType}</span>
                        {elapsed !== undefined && <span className="shrink-0 tabular-nums">+{formatElapsed(elapsed)}</span>}
                      </div>
                      <div className="text-[11px] text-muted-foreground tabular-nums">{formatTimestamp(record.timestamp, "yyyy-MM-dd HH:mm:ss.SSS")}</div>
                      {record.message && <div className="mt-0.5 line-clamp-2 text-xs text-muted-foreground">{record.message}</div>}
                    </div>
                  </button>
                </li>
              );
            })}
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
