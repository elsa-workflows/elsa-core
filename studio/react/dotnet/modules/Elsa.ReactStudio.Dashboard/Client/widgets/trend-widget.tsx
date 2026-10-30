"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Skeleton } from "@elsa-workflows/react-studio-ui/skeleton";
import type { DashboardRange } from "@elsa-workflows/react-studio-sdk";
import { TrendChart } from "../components/trend-chart";
import { WidgetCard, WidgetEmpty } from "../components/widget-card";
import { RANGE_LABELS } from "../format";
import { useWorkflowTrends } from "../hooks";

/** Started vs. outcome counts per bucket from `POST /dashboard/workflow-trends`. */
export function WorkflowTrendWidget({ range }: { range: DashboardRange }) {
  const _t = useTranslation();
  const trends = useWorkflowTrends(range);
  const granularity = trends.data?.granularity ?? (range === "1h" ? "minute" : range === "7d" ? "day" : "hour");
  const description =
    trends.capability === "fallback"
      ? translate("Trends need the Dashboard API (Elsa.Dashboard.Api), which this server does not expose.")
      : `${_t(RANGE_LABELS[range])}, per ${granularity === "minute" ? "5 minutes" : granularity}`;

  return (
    <WidgetCard
      title={_t("Workflow activity")}
      description={description}
      isLoading={trends.isLoading}
      error={trends.error}
      skeleton={<Skeleton className="h-48 w-full" />}
    >
      {trends.capability === "fallback" ? (
        <WidgetEmpty>{_t("Not available on this server.")}</WidgetEmpty>
      ) : trends.data ? (
        <TrendChart buckets={trends.data.buckets} granularity={trends.data.granularity} />
      ) : null}
    </WidgetCard>
  );
}
