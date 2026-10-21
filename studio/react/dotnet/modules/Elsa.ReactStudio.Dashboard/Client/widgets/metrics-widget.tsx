"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { CircleAlert, CircleCheck, CirclePause, CirclePlay, FileCode2, Siren, Timer } from "lucide-react";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import type { DashboardRange } from "@elsa-workflows/react-studio-sdk";
import { StatTile, type StatTone } from "../components/stat-tile";
import { formatCount, formatDuration, parseTimeSpan, RANGE_LABELS } from "../format";
import { useDashboardOverview, useFallbackInstanceCounts, useWorkflowDefinitionCount } from "../hooks";
import { DashboardLinks } from "../links";

/**
 * Workflow-instance stat tiles. Renders tiles directly (no card) so they flow into the metrics grid
 * next to tiles from other modules.
 *
 * Source: `overview.workflowInstances` (range-scoped) or, on servers without the Dashboard API,
 * `totalCount` of filtered instance list calls (all-time, no average duration).
 */
export function InstanceMetricsWidget({ range }: { range: DashboardRange }) {
  const _t = useTranslation();
  const overview = useDashboardOverview(range);
  const fallback = useFallbackInstanceCounts();
  const capability = overview.capability;
  const source = capability === "fallback" ? fallback : overview;

  if (source.error) {
    return (
      <div className="col-span-full">
        <ErrorAlert error={source.error} title={_t("Could not load workflow metrics")} />
      </div>
    );
  }

  const live = overview.data?.workflowInstances;
  const m = capability === "fallback" ? fallback.data : live;
  const caption = capability === "fallback" ? translate("All time") : _t(RANGE_LABELS[range]);
  const isLoading = source.isLoading || (capability !== undefined && m === undefined && !source.error);
  const avg = parseTimeSpan(live?.averageDuration);

  const tiles: { label: string; value: number | undefined; tone: StatTone; icon: typeof CirclePlay; href: string }[] = [
    { label: _t("Running"), value: m?.running, tone: "info", icon: CirclePlay, href: DashboardLinks.instancesWhere({ status: "Running" }) },
    { label: _t("Suspended"), value: m?.suspended, tone: "warning", icon: CirclePause, href: DashboardLinks.instancesWhere({ subStatus: "Suspended" }) },
    { label: _t("Faulted"), value: m?.faulted, tone: "danger", icon: CircleAlert, href: DashboardLinks.instancesWhere({ subStatus: "Faulted" }) },
    { label: _t("Completed"), value: m?.completed, tone: "success", icon: CircleCheck, href: DashboardLinks.instancesWhere({ subStatus: "Finished" }) },
    { label: _t("With incidents"), value: m?.incidentBearing, tone: "danger", icon: Siren, href: DashboardLinks.instancesWhere({ hasIncidents: true }) },
  ];

  return (
    <>
      {tiles.map((t) => (
        <StatTile key={t.label} label={t.label} value={formatCount(t.value)} caption={caption} tone={t.tone} icon={t.icon} href={t.href} isLoading={isLoading} />
      ))}
      <StatTile
        label={_t("Avg. duration")}
        value={capability === "fallback" ? "–" : formatDuration(avg)}
        caption={capability === "fallback" ? _t("Needs Dashboard API") : avg === undefined ? _t("Nothing finished") : _t(RANGE_LABELS[range])}
        tone="neutral"
        icon={Timer}
        isLoading={isLoading}
      />
    </>
  );
}

/** Number of workflow definitions; uses the core count endpoint so it works on every server. */
export function DefinitionCountWidget() {
  const _t = useTranslation();
  const query = useWorkflowDefinitionCount();
  if (query.error) {
    return (
      <div className="col-span-full">
        <ErrorAlert error={query.error} title={_t("Could not count workflow definitions")} />
      </div>
    );
  }
  return (
    <StatTile
      label={_t("Definitions")}
      value={formatCount(query.data)}
      caption={_t("Published")}
      tone="neutral"
      icon={FileCode2}
      href={DashboardLinks.definitions}
      isLoading={query.isPending}
    />
  );
}
