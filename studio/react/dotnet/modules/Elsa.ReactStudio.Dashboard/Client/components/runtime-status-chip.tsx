
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { message, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { Tooltip, TooltipContent, TooltipTrigger } from "@elsa-workflows/react-studio-ui/tooltip";
import type { DashboardRuntimeStatus } from "../api";
import type { DashboardCapability } from "../hooks";

const labels: Record<DashboardRuntimeStatus["status"], { label: string; tone: "success" | "warning" | "danger" | "neutral" }> = {
  AcceptingWork: { label: message("Accepting work"), tone: "success" },
  Paused: { label: message("Paused"), tone: "warning" },
  Draining: { label: message("Draining"), tone: "warning" },
  Unavailable: { label: message("Runtime unavailable"), tone: "neutral" },
};

/** Runtime state from the overview; on fallback servers it explains that the dashboard is limited. */
export function RuntimeStatusChip({ runtime, capability }: { runtime: DashboardRuntimeStatus | undefined; capability: DashboardCapability | undefined }) {
  const _t = useTranslation();
  if (capability === "fallback") {
    return (
      <Tooltip>
        <TooltipTrigger render={<span />}>
          <ToneBadge tone="neutral">{_t("Basic dashboard")}</ToneBadge>
        </TooltipTrigger>
        <TooltipContent>{_t("This server has no Dashboard API; figures are computed from the core endpoints.")}</TooltipContent>
      </Tooltip>
    );
  }
  if (!runtime) return null;
  const { label, tone } = labels[runtime.status] ?? labels.Unavailable;
  const details = [
    runtime.reason && runtime.reason !== "None" ? runtime.reason : undefined,
    runtime.activeExecutionCycleCount === 1 ? _t("{{count}} active cycle", { count: runtime.activeExecutionCycleCount }) : _t("{{count}} active cycles", { count: runtime.activeExecutionCycleCount }),
    runtime.failedIngressSourceCount > 0
      ? translate("{{value0}} of {{value1}} ingress sources failed", { value0: runtime.failedIngressSourceCount, value1: runtime.ingressSourceCount })
      : runtime.ingressSourceCount === 1 ? _t("{{count}} ingress source", { count: runtime.ingressSourceCount }) : _t("{{count}} ingress sources", { count: runtime.ingressSourceCount }),
  ].filter(Boolean);
  return (
    <Tooltip>
      <TooltipTrigger render={<span />}>
        <ToneBadge tone={runtime.failedIngressSourceCount > 0 && tone === "success" ? "warning" : tone}>
          <span className="relative flex size-1.5" aria-hidden>
            <span className={tone === "success" ? "absolute inline-flex size-full animate-ping rounded-full bg-current opacity-60" : "hidden"} />
            <span className="relative inline-flex size-1.5 rounded-full bg-current" />
          </span>
          {_t(label)}
        </ToneBadge>
      </TooltipTrigger>
      <TooltipContent>{details.join(" · ")}</TooltipContent>
    </Tooltip>
  );
}
