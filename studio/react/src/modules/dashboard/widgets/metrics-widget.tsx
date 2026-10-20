"use client";

import { CircleAlert, CircleCheck, CirclePause, CirclePlay, FileCode2, Siren, Timer } from "lucide-react";
import { ErrorAlert } from "@/components/common/error-alert";
import type { DashboardRange } from "@/lib/modules/types";
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
  const overview = useDashboardOverview(range);
  const fallback = useFallbackInstanceCounts();
  const capability = overview.capability;
  const source = capability === "fallback" ? fallback : overview;

  if (source.error) {
    return (
      <div className="col-span-full">
        <ErrorAlert error={source.error} title="Could not load workflow metrics" />
      </div>
    );
  }

  const live = overview.data?.workflowInstances;
  const m = capability === "fallback" ? fallback.data : live;
  const caption = capability === "fallback" ? "All time" : RANGE_LABELS[range];
  const isLoading = source.isLoading || (capability !== undefined && m === undefined && !source.error);
  const avg = parseTimeSpan(live?.averageDuration);

  const tiles: { label: string; value: number | undefined; tone: StatTone; icon: typeof CirclePlay; href: string }[] = [
    { label: "Running", value: m?.running, tone: "info", icon: CirclePlay, href: DashboardLinks.instancesWhere({ status: "Running" }) },
    { label: "Suspended", value: m?.suspended, tone: "warning", icon: CirclePause, href: DashboardLinks.instancesWhere({ subStatus: "Suspended" }) },
    { label: "Faulted", value: m?.faulted, tone: "danger", icon: CircleAlert, href: DashboardLinks.instancesWhere({ subStatus: "Faulted" }) },
    { label: "Completed", value: m?.completed, tone: "success", icon: CircleCheck, href: DashboardLinks.instancesWhere({ subStatus: "Finished" }) },
    { label: "With incidents", value: m?.incidentBearing, tone: "danger", icon: Siren, href: DashboardLinks.instancesWhere({ hasIncidents: true }) },
  ];

  return (
    <>
      {tiles.map((t) => (
        <StatTile key={t.label} label={t.label} value={formatCount(t.value)} caption={caption} tone={t.tone} icon={t.icon} href={t.href} isLoading={isLoading} />
      ))}
      <StatTile
        label="Avg. duration"
        value={capability === "fallback" ? "–" : formatDuration(avg)}
        caption={capability === "fallback" ? "Needs Dashboard API" : avg === undefined ? "Nothing finished" : RANGE_LABELS[range]}
        tone="neutral"
        icon={Timer}
        isLoading={isLoading}
      />
    </>
  );
}

/** Number of workflow definitions; uses the core count endpoint so it works on every server. */
export function DefinitionCountWidget() {
  const query = useWorkflowDefinitionCount();
  if (query.error) {
    return (
      <div className="col-span-full">
        <ErrorAlert error={query.error} title="Could not count workflow definitions" />
      </div>
    );
  }
  return (
    <StatTile
      label="Definitions"
      value={formatCount(query.data)}
      caption="Published"
      tone="neutral"
      icon={FileCode2}
      href={DashboardLinks.definitions}
      isLoading={query.isPending}
    />
  );
}
