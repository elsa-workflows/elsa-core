"use client";

import { ClipboardCheck, Loader2 } from "lucide-react";
import type { ReactNode } from "react";
import { EmptyState } from "@/components/common/empty-state";
import { describeError } from "@/components/common/error-alert";
import { useUserTaskCapabilities } from "../hooks";

/** Renders children only when `GET /user-tasks/capabilities` says the feature is usable; explains otherwise. */
export function UserTasksGate({ children }: { children: ReactNode }) {
  const { enabled, isResolved, capabilities, error } = useUserTaskCapabilities();
  if (!isResolved) {
    return (
      <div className="flex items-center gap-2 text-sm text-muted-foreground py-10 justify-center">
        <Loader2 className="size-4 animate-spin" /> Checking user task capabilities…
      </div>
    );
  }
  if (!enabled) {
    const reason = error
      ? `The server did not report user task capabilities (${describeError(error)}).`
      : capabilities && !capabilities.enabled
        ? "User tasks are disabled on this server."
        : "Your account cannot list or read user tasks.";
    return <EmptyState icon={ClipboardCheck} title="User tasks are not available" description={reason} />;
  }
  return <>{children}</>;
}
