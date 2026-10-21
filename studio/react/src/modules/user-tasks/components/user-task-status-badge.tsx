import { ToneBadge } from "@/components/common/status-badge";
import type { UserTaskStatus } from "../api";

type Tone = "neutral" | "info" | "success" | "warning" | "danger";

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
  if (!status) return null;
  return <ToneBadge tone={toneFor(status)}>{status}</ToneBadge>;
}

export function PriorityBadge({ priority }: { priority: number }) {
  const tone: Tone = priority >= 80 ? "danger" : priority >= 60 ? "warning" : priority <= 20 ? "neutral" : "info";
  return (
    <ToneBadge tone={tone} className="tabular-nums">
      P{priority}
    </ToneBadge>
  );
}
