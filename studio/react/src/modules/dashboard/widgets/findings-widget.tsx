"use client";

import Link from "next/link";
import { ArrowUpRight, CircleAlert, CircleCheck, Info, OctagonAlert, TriangleAlert, type LucideIcon } from "lucide-react";
import type { DashboardRange } from "@/lib/modules/types";
import { cn } from "@/lib/utils";
import type { DashboardFinding, DashboardFindingSeverity } from "../api";
import { SourceNote, WidgetCard, WidgetEmpty } from "../components/widget-card";
import { useFallbackInstanceCounts, useNeedsAttention } from "../hooks";
import { DashboardLinks } from "../links";

const severity: Record<DashboardFindingSeverity, { icon: LucideIcon; className: string; rank: number }> = {
  Critical: { icon: OctagonAlert, className: "text-red-600 dark:text-red-400", rank: 0 },
  Error: { icon: CircleAlert, className: "text-red-600 dark:text-red-400", rank: 1 },
  Warning: { icon: TriangleAlert, className: "text-amber-600 dark:text-amber-400", rank: 2 },
  Info: { icon: Info, className: "text-sky-600 dark:text-sky-400", rank: 3 },
  Success: { icon: CircleCheck, className: "text-emerald-600 dark:text-emerald-400", rank: 4 },
};

/** Maps a server finding target to a studio route; unknown targets render without a link. */
function hrefFor(f: DashboardFinding): string | undefined {
  if (f.targetKind === "WorkflowInstances") {
    switch (f.target) {
      case "faulted":
        return DashboardLinks.instancesWhere({ subStatus: "Faulted" });
      case "interrupted":
        return DashboardLinks.instancesWhere({ subStatus: "Interrupted" });
      case "incidents":
        return DashboardLinks.instancesWhere({ hasIncidents: true });
      case "suspended":
        return DashboardLinks.instancesWhere({ subStatus: "Suspended" });
    }
  }
  if (f.targetKind === "WorkflowInstance" && f.target) return DashboardLinks.instance(f.target);
  if (f.targetKind === "WorkflowDefinition" && f.target) return DashboardLinks.definition(f.target);
  return undefined;
}

/** `GET /dashboard/needs-attention`; on fallback servers derived from the all-time instance counts. */
export function NeedsAttentionWidget({ range }: { range: DashboardRange }) {
  const attention = useNeedsAttention(range);
  const counts = useFallbackInstanceCounts();
  const capability = attention.capability;
  const source = capability === "fallback" ? counts : attention;

  const findings: DashboardFinding[] =
    capability === "fallback"
      ? fallbackFindings(counts.data)
      : [...(attention.data?.findings ?? [])].sort((a, b) => severity[a.severity].rank - severity[b.severity].rank || a.priority - b.priority);

  return (
    <WidgetCard title="Needs attention" isLoading={source.isLoading} error={source.error} contentClassName="flex flex-col gap-3">
      {findings.length === 0 ? (
        <WidgetEmpty>
          <CircleCheck className="mr-1.5 inline size-4 text-emerald-600 dark:text-emerald-400" aria-hidden />
          Nothing needs attention.
        </WidgetEmpty>
      ) : (
        <ul className="flex flex-col divide-y">
          {findings.map((f) => (
            <FindingRow key={f.id} finding={f} />
          ))}
        </ul>
      )}
      <SourceNote capability={capability}>Based on all-time instance counts; the Dashboard API adds runtime and range-scoped findings.</SourceNote>
    </WidgetCard>
  );
}

function FindingRow({ finding }: { finding: DashboardFinding }) {
  const { icon: Icon, className } = severity[finding.severity] ?? severity.Info;
  const href = hrefFor(finding);
  const inner = (
    <>
      <Icon className={cn("mt-0.5 size-4 shrink-0", className)} aria-label={finding.severity} />
      <span className="min-w-0 flex-1 text-sm">{finding.message}</span>
      {href && <ArrowUpRight className="size-3.5 shrink-0 text-muted-foreground opacity-0 transition-opacity group-hover/finding:opacity-100" aria-hidden />}
    </>
  );
  return (
    <li className="group/finding">
      {href ? (
        <Link href={href} className="-mx-2 flex items-start gap-2 rounded-md px-2 py-2 transition-colors hover:bg-muted/50">
          {inner}
        </Link>
      ) : (
        <div className="flex items-start gap-2 py-2">{inner}</div>
      )}
    </li>
  );
}

function fallbackFindings(counts: ReturnType<typeof useFallbackInstanceCounts>["data"]): DashboardFinding[] {
  if (!counts) return [];
  const out: DashboardFinding[] = [];
  if (counts.faulted > 0)
    out.push({ id: "workflow-faults", severity: "Error", message: `${counts.faulted} workflow${counts.faulted === 1 ? "" : "s"} faulted`, targetKind: "WorkflowInstances", target: "faulted", priority: 10 });
  if (counts.incidentBearing > 0)
    out.push({ id: "workflow-incidents", severity: "Error", message: `${counts.incidentBearing} workflow${counts.incidentBearing === 1 ? " has" : "s have"} incidents`, targetKind: "WorkflowInstances", target: "incidents", priority: 20 });
  if (counts.suspended > 0)
    out.push({ id: "workflow-suspended", severity: "Info", message: `${counts.suspended} workflow${counts.suspended === 1 ? " is" : "s are"} suspended, waiting for input`, targetKind: "WorkflowInstances", target: "suspended", priority: 30 });
  return out;
}
