"use client";

import Link from "next/link";
import { differenceInMilliseconds, parseISO } from "date-fns";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/common/status-badge";
import { Timestamp } from "@/components/common/timestamp";
import type { DashboardRange } from "@/lib/modules/types";
import type { WorkflowInstanceSummary } from "@/lib/api/types/workflow-instances";
import type { DashboardRecentActivityItem } from "../api";
import { LinesSkeleton, SourceNote, WidgetCard, WidgetEmpty } from "../components/widget-card";
import { formatDuration, parseTimeSpan, RANGE_LABELS } from "../format";
import { useFallbackRecentInstances, useRecentActivity } from "../hooks";
import { DashboardLinks } from "../links";

interface ActivityRow {
  id: string;
  name: string;
  subStatus: string;
  incidentCount: number;
  durationMs?: number;
  at: string;
}

function fromApi(item: DashboardRecentActivityItem): ActivityRow {
  return {
    id: item.instanceId,
    name: item.workflowName || item.definitionId,
    subStatus: item.subStatus,
    incidentCount: item.incidentCount,
    durationMs: parseTimeSpan(item.duration),
    at: item.updatedAt ?? item.finishedAt ?? item.createdAt,
  };
}

function fromInstance(i: WorkflowInstanceSummary): ActivityRow {
  return {
    id: i.id,
    name: i.name || i.definitionId,
    subStatus: i.subStatus,
    incidentCount: i.incidentCount,
    durationMs: i.finishedAt ? differenceInMilliseconds(parseISO(i.finishedAt), parseISO(i.createdAt)) : undefined,
    at: i.updatedAt ?? i.finishedAt ?? i.createdAt,
  };
}

/** `GET /dashboard/recent-activity`, or the 10 newest instances from the core list endpoint. */
export function RecentActivityWidget({ range }: { range: DashboardRange }) {
  // Enough rows to fill the fixed card height on a busy server; the list scrolls past that.
  const activity = useRecentActivity(range, 14);
  const fallback = useFallbackRecentInstances(14);
  const capability = activity.capability;
  const source = capability === "fallback" ? fallback : activity;
  const rows: ActivityRow[] = capability === "fallback" ? (fallback.data ?? []).map(fromInstance) : (activity.data?.items ?? []).map(fromApi);

  return (
    <WidgetCard
      title="Recent activity"
      description={capability === "fallback" ? "Newest workflow instances" : `Workflow instances updated in the ${RANGE_LABELS[range].toLowerCase()}`}
      action={
        <Button variant="ghost" size="sm" nativeButton={false} render={<Link href={DashboardLinks.instances} />}>
          View all
        </Button>
      }
      isLoading={source.isLoading}
      error={source.error}
      skeleton={<LinesSkeleton lines={6} />}
      scroll
      contentClassName="flex flex-col gap-3"
    >
      {rows.length === 0 ? (
        <WidgetEmpty>No workflow activity yet.</WidgetEmpty>
      ) : (
        <ul className="-mx-2 flex flex-col">
          {rows.map((r) => (
            <li key={r.id}>
              <Link
                href={DashboardLinks.instance(r.id)}
                className="flex items-center gap-3 rounded-md px-2 py-1.5 text-sm transition-colors hover:bg-muted/50"
              >
                <StatusBadge status={r.subStatus} className="w-24 justify-center" />
                <span className="min-w-0 flex-1 truncate font-medium">{r.name}</span>
                {r.incidentCount > 0 && (
                  <span className="shrink-0 text-xs text-red-600 dark:text-red-400 tabular-nums">
                    {r.incidentCount} incident{r.incidentCount === 1 ? "" : "s"}
                  </span>
                )}
                <span className="hidden w-16 shrink-0 text-right text-xs text-muted-foreground tabular-nums sm:inline">{formatDuration(r.durationMs)}</span>
                <span className="w-28 shrink-0 text-right text-xs text-muted-foreground">
                  <Timestamp value={r.at} relative />
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
      <SourceNote capability={capability}>Showing the newest instances; the Dashboard API scopes this list to the selected range.</SourceNote>
    </WidgetCard>
  );
}
