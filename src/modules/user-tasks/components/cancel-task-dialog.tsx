"use client";

import { Loader2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { Field } from "@/components/common/field";

export function CancelTaskDialog({
  open,
  onOpenChange,
  onSubmit,
  isPending,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (reason: string) => Promise<unknown>;
  isPending: boolean;
}) {
  const [reason, setReason] = useState("");
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Cancel task</DialogTitle>
          <DialogDescription>The task ends with the “Cancelled” outcome and the workflow resumes accordingly.</DialogDescription>
        </DialogHeader>
        <Field label="Reason" required htmlFor="cancel-reason">
          <Textarea id="cancel-reason" rows={3} value={reason} onChange={(e) => setReason(e.target.value)} />
        </Field>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Keep task
          </Button>
          <Button variant="destructive" disabled={isPending || !reason.trim()} onClick={() => onSubmit(reason.trim())}>
            {isPending && <Loader2 className="animate-spin" data-icon="inline-start" />}
            Cancel task
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
