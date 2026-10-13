"use client";

import type { ReactNode } from "react";
import { useAlterationDialogs } from "../dialog-store";
import { AlterInstanceDialog } from "./alter-instance-dialog";
import { RetryFaultedDialog } from "./retry-faulted-dialog";

/** Module provider: mounts the alteration dialogs once so list/viewer actions can open them. */
export function AlterationDialogHost({ children }: { children: ReactNode }) {
  const { alter, retry, close } = useAlterationDialogs();
  return (
    <>
      {children}
      {alter && (
        <AlterInstanceDialog
          key={alter.targets.map((t) => t.workflowInstanceId).join("|")}
          targets={alter.targets}
          open
          onOpenChange={(o) => !o && close()}
          onDone={alter.onDone}
        />
      )}
      {retry && (
        <RetryFaultedDialog
          key={retry.workflowInstanceIds.join("|")}
          open
          onOpenChange={(o) => !o && close()}
          initialInstanceIds={retry.workflowInstanceIds}
          onDone={retry.onDone}
        />
      )}
    </>
  );
}
