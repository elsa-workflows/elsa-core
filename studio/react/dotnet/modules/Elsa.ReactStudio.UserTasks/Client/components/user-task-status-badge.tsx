
import { message, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import type { UserTaskStatus } from "../api";

type Tone = "neutral" | "info" | "success" | "warning" | "danger";
const statusMessages: Record<string, string> = {
  Unassigned: message("Unassigned"), Available: message("Available"), Assigned: message("Assigned"),
  Completing: message("Completing"), TimingOut: message("TimingOut"), Cancelling: message("Cancelling"),
  Completed: message("Completed"), TimedOut: message("TimedOut"), Cancelled: message("Cancelled"),
};

function toneFor(status: UserTaskStatus | string): Tone {
  switch (status) {
    case "Assigned":
    case "Completing":
      return "info";
    case "Completed":
      return "success";
    case "Available":
    case "Unassigned":
    case "TimingOut":
    case "Cancelling":
      return "warning";
    case "TimedOut":
      return "danger";
    default:
      return "neutral";
  }
}

export function UserTaskStatusBadge({ status }: { status: UserTaskStatus | string | undefined }) {
  const t = useTranslation();
  if (!status) return null;
  return <ToneBadge tone={toneFor(status)}>{statusMessages[status] ? t(statusMessages[status]) : status}</ToneBadge>;
}

export function PriorityBadge({ priority }: { priority: number }) {
  const _t = useTranslation();
  const tone: Tone = priority >= 80 ? "danger" : priority >= 60 ? "warning" : priority <= 20 ? "neutral" : "info";
  return (
    <ToneBadge tone={tone} className="tabular-nums">
      {_t("P")}{priority}
    </ToneBadge>
  );
}
