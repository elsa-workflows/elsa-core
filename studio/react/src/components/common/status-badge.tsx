import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { ActivityStatus, WorkflowStatus, WorkflowSubStatus } from "@/lib/api/types/workflow-instances";

type Tone = "neutral" | "info" | "success" | "warning" | "danger";

const toneClass: Record<Tone, string> = {
  neutral: "border-border bg-card text-muted-foreground [--dot:var(--status-neutral)]",
  info: "border-status-info/30 bg-status-info/10 text-status-info [--dot:var(--status-info)]",
  success: "border-status-success/30 bg-status-success/10 text-status-success [--dot:var(--status-success)]",
  warning: "border-status-warning/30 bg-status-warning/10 text-status-warning [--dot:var(--status-warning)]",
  danger: "border-status-danger/30 bg-status-danger/10 text-status-danger [--dot:var(--status-danger)]",
};

function Dot() {
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
  if (!status) return null;
  return (
    <Badge variant="outline" className={cn("gap-1.5 font-medium", toneClass[tone ?? toneForSubStatus(status)], className)}>
      <Dot />
      {status}
    </Badge>
  );
}

export function ToneBadge({ tone, children, className }: { tone: Tone; children: React.ReactNode; className?: string }) {
  return (
    <Badge variant="outline" className={cn("gap-1.5 font-medium", toneClass[tone], className)}>
      <Dot />
      {children}
    </Badge>
  );
}
