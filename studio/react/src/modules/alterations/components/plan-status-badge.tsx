import { ToneBadge } from "@/components/common/status-badge";
import type { AlterationJobStatus, AlterationPlanStatus } from "../api";

type Tone = "neutral" | "info" | "success" | "warning" | "danger";

export function planTone(status: AlterationPlanStatus | AlterationJobStatus | undefined): Tone {
  switch (status) {
    case "Completed":
      return "success";
    case "Failed":
      return "danger";
    case "Running":
    case "Dispatching":
    case "Generating":
      return "info";
    case "Pending":
      return "warning";
    default:
      return "neutral";
  }
}

export function PlanStatusBadge({ status }: { status: AlterationPlanStatus | AlterationJobStatus | undefined }) {
  if (!status) return null;
  return <ToneBadge tone={planTone(status)}>{status}</ToneBadge>;
}
