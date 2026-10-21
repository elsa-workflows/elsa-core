"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ClipboardCheck, Loader2 } from "lucide-react";
import type { ReactNode } from "react";
import { EmptyState } from "@elsa-workflows/react-studio-ui/common/empty-state";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { useUserTaskCapabilities } from "../hooks";

/** Renders children only when `GET /user-tasks/capabilities` says the feature is usable; explains otherwise. */
export function UserTasksGate({ children }: { children: ReactNode }) {
  const _t = useTranslation();
  const { enabled, isResolved, capabilities, error } = useUserTaskCapabilities();
  if (!isResolved) {
    return (
      <div className="flex items-center gap-2 text-sm text-muted-foreground py-10 justify-center">
        <Loader2 className="size-4 animate-spin" /> {_t("Checking user task capabilities…")}</div>
    );
  }
  if (!enabled) {
    const reason = error
      ? translate("The server did not report user task capabilities ({{value0}}).", { value0: describeError(error) })
      : capabilities && !capabilities.enabled
        ? translate("User tasks are disabled on this server.")
        : translate("Your account cannot list or read user tasks.");
    return <EmptyState icon={ClipboardCheck} title={_t("User tasks are not available")} description={reason} />;
  }
  return <>{children}</>;
}
