"use client";

import Link from "next/link";
import { useState } from "react";
import { SimpleSelect } from "@/components/common/simple-select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { DashboardRange } from "@/lib/modules/types";
import type { DashboardHotspotMetric } from "../api";
import { LinesSkeleton, WidgetCard, WidgetEmpty } from "../components/widget-card";
import { formatCount, formatDuration, parseTimeSpan, RANGE_LABELS } from "../format";
import { useWorkflowHotspots } from "../hooks";
import { DashboardLinks } from "../links";

const METRICS: { value: DashboardHotspotMetric; label: string; column: string }[] = [
  { value: "Faults", label: "Faults", column: "Faults" },
  { value: "Incidents", label: "Incidents", column: "Incidents" },
  { value: "Executions", label: "Executions", column: "Runs" },
  { value: "Duration", label: "Duration", column: "Avg. duration" },
];

/**
 * `POST /dashboard/workflow-hotspots`: the definitions ranking highest on the chosen metric.
 * Hidden on servers without the Dashboard API (there is no cheap core-API equivalent).
 */
export function WorkflowHotspotsWidget({ range }: { range: DashboardRange }) {
  const [metric, setMetric] = useState<DashboardHotspotMetric>("Faults");
  const hotspots = useWorkflowHotspots(range, metric);
  if (hotspots.capability === "fallback") return null;

  const items = hotspots.data?.items ?? [];
  const column = METRICS.find((m) => m.value === metric)?.column ?? metric;

  return (
    <WidgetCard
      title="Workflow hotspots"
      description={RANGE_LABELS[range]}
      action={<SimpleSelect size="sm" className="w-32" value={metric} options={METRICS} onChange={(v) => v && setMetric(v)} />}
      isLoading={hotspots.isLoading}
      error={hotspots.error}
      skeleton={<LinesSkeleton lines={5} />}
      contentClassName="px-0"
    >
      {items.length === 0 ? (
        <WidgetEmpty>No hotspots in this range.</WidgetEmpty>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="pl-4">Workflow</TableHead>
              <TableHead className="text-right">{column}</TableHead>
              {metric !== "Duration" && <TableHead className="hidden text-right pr-4 sm:table-cell">Avg. duration</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map((h) => {
              const avg = formatDuration(parseTimeSpan(h.averageDuration));
              return (
                <TableRow key={h.definitionId}>
                  <TableCell className="max-w-0 pl-4">
                    <Link href={DashboardLinks.instancesWhere({ definitionId: h.definitionId })} className="block truncate font-medium hover:underline">
                      {h.workflowName || h.definitionId}
                    </Link>
                  </TableCell>
                  <TableCell className="text-right tabular-nums">{metric === "Duration" ? avg : formatCount(h.value)}</TableCell>
                  {metric !== "Duration" && <TableCell className="hidden text-right pr-4 text-muted-foreground tabular-nums sm:table-cell">{avg}</TableCell>}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}
    </WidgetCard>
  );
}
