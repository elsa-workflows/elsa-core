"use client";

import type { DashboardRange } from "@/lib/modules/types";
import { BarList } from "../components/bar-list";
import { WidgetCard, WidgetEmpty } from "../components/widget-card";
import { useInstancesPerWorkflow } from "../hooks";
import { DashboardLinks } from "../links";

/** Instances per published workflow, with running and faulted shares inside each bar. */
export function InstancesPerWorkflowWidget({ range }: { range: DashboardRange }) {
  void range; // counts are all-time; the range toggle does not apply to this view
  const query = useInstancesPerWorkflow(8);
  const rows = (query.data ?? []).map((r) => ({
    label: r.name,
    value: r.total,
    href: DashboardLinks.instancesWhere({ definitionId: r.definitionId }),
    hint: `${r.total} instances · ${r.running} running · ${r.faulted} faulted`,
    segments: [
      { label: "Finished / other", value: Math.max(0, r.total - r.running - r.faulted), color: "var(--primary)" },
      { label: "Running", value: r.running, color: "var(--status-info)" },
      { label: "Faulted", value: r.faulted, color: "var(--status-danger)" },
    ],
  }));
  return (
    <WidgetCard title="Instances per workflow" description="Published workflows · all time" isLoading={query.isLoading} error={query.error}>
      {rows.length === 0 ? (
        <WidgetEmpty>No published workflows have run yet.</WidgetEmpty>
      ) : (
        <>
          <BarList rows={rows} />
          <div className="mt-3 flex flex-wrap gap-3 text-xs text-muted-foreground">
            <span className="inline-flex items-center gap-1.5"><span className="size-2.5 rounded-full bg-primary" /> finished / other</span>
            <span className="inline-flex items-center gap-1.5"><span className="size-2.5 rounded-full bg-status-info" /> running</span>
            <span className="inline-flex items-center gap-1.5"><span className="size-2.5 rounded-full bg-status-danger" /> faulted</span>
          </div>
        </>
      )}
    </WidgetCard>
  );
}
