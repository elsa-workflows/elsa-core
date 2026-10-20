import { ToneBadge } from "@/components/common/status-badge";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { DashboardRuntimeStatus } from "../api";
import type { DashboardCapability } from "../hooks";

const labels: Record<DashboardRuntimeStatus["status"], { label: string; tone: "success" | "warning" | "danger" | "neutral" }> = {
  AcceptingWork: { label: "Accepting work", tone: "success" },
  Paused: { label: "Paused", tone: "warning" },
  Draining: { label: "Draining", tone: "warning" },
  Unavailable: { label: "Runtime unavailable", tone: "neutral" },
};

/** Runtime state from the overview; on fallback servers it explains that the dashboard is limited. */
export function RuntimeStatusChip({ runtime, capability }: { runtime: DashboardRuntimeStatus | undefined; capability: DashboardCapability | undefined }) {
  if (capability === "fallback") {
    return (
      <Tooltip>
        <TooltipTrigger render={<span />}>
          <ToneBadge tone="neutral">Basic dashboard</ToneBadge>
        </TooltipTrigger>
        <TooltipContent>This server has no Dashboard API; figures are computed from the core endpoints.</TooltipContent>
      </Tooltip>
    );
  }
  if (!runtime) return null;
  const { label, tone } = labels[runtime.status] ?? labels.Unavailable;
  const details = [
    runtime.reason && runtime.reason !== "None" ? runtime.reason : undefined,
    `${runtime.activeExecutionCycleCount} active cycle${runtime.activeExecutionCycleCount === 1 ? "" : "s"}`,
    runtime.failedIngressSourceCount > 0
      ? `${runtime.failedIngressSourceCount} of ${runtime.ingressSourceCount} ingress sources failed`
      : `${runtime.ingressSourceCount} ingress source${runtime.ingressSourceCount === 1 ? "" : "s"}`,
  ].filter(Boolean);
  return (
    <Tooltip>
      <TooltipTrigger render={<span />}>
        <ToneBadge tone={runtime.failedIngressSourceCount > 0 && tone === "success" ? "warning" : tone}>
          <span className="relative flex size-1.5" aria-hidden>
            <span className={tone === "success" ? "absolute inline-flex size-full animate-ping rounded-full bg-current opacity-60" : "hidden"} />
            <span className="relative inline-flex size-1.5 rounded-full bg-current" />
          </span>
          {label}
        </ToneBadge>
      </TooltipTrigger>
      <TooltipContent>{details.join(" · ")}</TooltipContent>
    </Tooltip>
  );
}
