"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import type { DashboardRange } from "@elsa-workflows/react-studio-sdk";
import { DonutChart } from "../components/donut-chart";
import { SourceNote, WidgetCard } from "../components/widget-card";
import { useDashboardOverview, useFallbackInstanceCounts } from "../hooks";
import { DashboardLinks } from "../links";
import { RANGE_LABELS } from "../format";

/** Instances by status as a donut, from the overview (or the core-API counts on older servers). */
export function StatusBreakdownWidget({ range }: { range: DashboardRange }) {
  const _t = useTranslation();
  const overview = useDashboardOverview(range);
  const fallback = useFallbackInstanceCounts();
  const capability = overview.capability ?? fallback.capability;
  const counts = capability === "fallback" ? fallback.data : overview.data?.workflowInstances;
  const isLoading = capability === "fallback" ? fallback.isLoading : overview.isLoading;
  const error = capability === "fallback" ? fallback.error : overview.error;

  const slices = counts
    ? [
        { label: _t("Executing"), value: Math.max(0, counts.running - counts.suspended), color: "var(--status-info)", href: DashboardLinks.instancesWhere({ subStatus: "Executing" }) },
        { label: _t("Suspended"), value: counts.suspended, color: "var(--status-warning)", href: DashboardLinks.instancesWhere({ subStatus: "Suspended" }) },
        { label: _t("Completed"), value: counts.completed, color: "var(--status-success)", href: DashboardLinks.instancesWhere({ subStatus: "Finished" }) },
        { label: _t("Faulted"), value: counts.faulted, color: "var(--status-danger)", href: DashboardLinks.instancesWhere({ subStatus: "Faulted" }) },
      ]
    : [];

  return (
    <WidgetCard
      title={_t("Instances by status")}
      description={capability === "fallback" ? _t("All time") : _t(RANGE_LABELS[range])}
      isLoading={isLoading}
      error={error}
      contentClassName="flex flex-col justify-center gap-3"
    >
      <DonutChart slices={slices} centerLabel="instances" />
      <SourceNote capability={capability}>{_t("Counts cover all instances; the Dashboard API would scope them to the selected range.")}</SourceNote>
    </WidgetCard>
  );
}
