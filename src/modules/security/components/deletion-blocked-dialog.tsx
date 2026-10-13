"use client";

import { ShieldAlert } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import type { IdentityRole, RoleDeletionImpact } from "../api";

/** Explains why the server refuses to delete a role (`canDelete === false`). */
export function DeletionBlockedDialog({ state, onClose }: { state: { role: IdentityRole; impact: RoleDeletionImpact } | null; onClose: () => void }) {
  const impact = state?.impact;
  const configRefs = impact?.configurationReferences.length ?? 0;
  const editableRefs = impact?.editableReferences.length ?? 0;
  return (
    <Dialog open={!!state} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Cannot delete role {state?.role.name}</DialogTitle>
          <DialogDescription>The server reports that this role is still referenced and refuses the deletion.</DialogDescription>
        </DialogHeader>
        <Alert variant="destructive">
          <ShieldAlert />
          <AlertTitle>Role is in use</AlertTitle>
          <AlertDescription>
            <ul className="list-disc pl-4">
              {configRefs > 0 && <li>{configRefs} reference(s) in server configuration (users or applications defined in settings) must be changed by an administrator first.</li>}
              {editableRefs > 0 && (
                <li>
                  {editableRefs} editable reference(s) (users or applications in the identity store).
                  {impact?.canRemediate ? " Remove the role from those principals and try again." : ""}
                </li>
              )}
              {impact?.warnings.map((w, i) => (
                <li key={i}>{w}</li>
              ))}
              {configRefs === 0 && editableRefs === 0 && (impact?.warnings.length ?? 0) === 0 && <li>No further details were provided.</li>}
            </ul>
          </AlertDescription>
        </Alert>
        <DialogFooter>
          <Button onClick={onClose}>Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
