"use client";

import { Skeleton } from "@/components/ui/skeleton";
import type { DashboardRange } from "@/lib/modules/types";
import { TrendChart } from "../components/trend-chart";
import { WidgetCard, WidgetEmpty } from "../components/widget-card";
import { RANGE_LABELS } from "../format";
import { useWorkflowTrends } from "../hooks";

/** Started vs. outcome counts per bucket from `POST /dashboard/workflow-trends`. */
export function WorkflowTrendWidget({ range }: { range: DashboardRange }) {
  const trends = useWorkflowTrends(range);
  const granularity = trends.data?.granularity ?? (range === "1h" ? "minute" : range === "7d" ? "day" : "hour");
  const description =
    trends.capability === "fallback"
      ? "Trends need the Dashboard API (Elsa.Dashboard.Api), which this server does not expose."
      : `${RANGE_LABELS[range]}, per ${granularity === "minute" ? "5 minutes" : granularity}`;

  return (
    <WidgetCard
      title="Workflow activity"
      description={description}
      isLoading={trends.isLoading}
      error={trends.error}
      skeleton={<Skeleton className="h-48 w-full" />}
    >
      {trends.capability === "fallback" ? (
        <WidgetEmpty>Not available on this server.</WidgetEmpty>
      ) : trends.data ? (
        <TrendChart buckets={trends.data.buckets} granularity={trends.data.granularity} />
      ) : null}
    </WidgetCard>
  );
}
