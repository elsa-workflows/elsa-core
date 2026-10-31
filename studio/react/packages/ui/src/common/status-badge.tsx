
import { message, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { Badge } from "@elsa-workflows/react-studio-ui/badge";
import { cn } from "../utils.js";
import type { ActivityStatus, WorkflowStatus, WorkflowSubStatus } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";

type Tone = "neutral" | "info" | "success" | "warning" | "danger";
const statusMessages: Record<string, string> = {
  Pending: message("Pending"), Running: message("Running"), Completed: message("Completed"),
  Canceled: message("Canceled"), Faulted: message("Faulted"), Executing: message("Executing"),
  Suspended: message("Suspended"), Finished: message("Finished"), Cancelled: message("Cancelled"), Interrupted: message("Interrupted"),
};

const toneClass: Record<Tone, string> = {
  neutral: "border-border bg-card text-muted-foreground [--dot:var(--status-neutral)]",
  info: "border-status-info/30 bg-status-info/10 text-status-info [--dot:var(--status-info)]",
  success: "border-status-success/30 bg-status-success/10 text-status-success [--dot:var(--status-success)]",
  warning: "border-status-warning/30 bg-status-warning/10 text-status-warning [--dot:var(--status-warning)]",
  danger: "border-status-danger/30 bg-status-danger/10 text-status-danger [--dot:var(--status-danger)]",
};

function Dot() {
  useTranslation();
  return <span className="size-1.5 shrink-0 rounded-full" style={{ background: "var(--dot)" }} aria-hidden />;
}

export function toneForSubStatus(s: WorkflowSubStatus | ActivityStatus | string | undefined): Tone {
  switch (s) {
    case "Executing":
    case "Running":
      return "info";
    case "Finished":
    case "Completed":
      return "success";
    case "Suspended":
    case "Pending":
    case "Interrupted":
      return "warning";
    case "Faulted":
      return "danger";
    case "Cancelled":
    case "Canceled":
    default:
      return "neutral";
  }
}

export function StatusBadge({
  status,
  className,
  tone,
}: {
  status: WorkflowStatus | WorkflowSubStatus | ActivityStatus | string | undefined;
  className?: string;
  tone?: Tone;
}) {
  const t = useTranslation();
  if (!status) return null;
  return (
    <Badge variant="outline" className={cn("gap-1.5 font-medium", toneClass[tone ?? toneForSubStatus(status)], className)}>
      <Dot />
      {statusMessages[status] ? t(statusMessages[status]) : status}
    </Badge>
  );
}

export function ToneBadge({ tone, children, className }: { tone: Tone; children: React.ReactNode; className?: string }) {
  useTranslation();
  return (
    <Badge variant="outline" className={cn("gap-1.5 font-medium", toneClass[tone], className)}>
      <Dot />
      {children}
    </Badge>
  );
}
