"use client";

import { Loader2, RefreshCw } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Field } from "@/components/common/field";
import { ErrorAlert, describeError } from "@/components/common/error-alert";
import type { RunAlterationsResult } from "../api";
import { useRetryWorkflows } from "../hooks";
import { AlterationResults } from "./alteration-results";

function splitIds(text: string) {
  return Array.from(new Set(text.split(/[\s,]+/).map((s) => s.trim()).filter(Boolean)));
}

/** Bulk helper for `POST /alterations/workflows/retry`: paste instance ids, optionally restrict to activity ids. */
export function RetryFaultedDialog({
  open,
  onOpenChange,
  initialInstanceIds = [],
  onDone,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialInstanceIds?: string[];
  onDone?: () => void;
}) {
  const [instanceText, setInstanceText] = useState(initialInstanceIds.join("\n"));
  const [activityText, setActivityText] = useState("");
  const [results, setResults] = useState<RunAlterationsResult[] | null>(null);
  const retry = useRetryWorkflows();

  const onRetry = async () => {
    const workflowInstanceIds = splitIds(instanceText);
    if (workflowInstanceIds.length === 0) {
      toast.error("Enter at least one workflow instance id.");
      return;
    }
    const activityIds = splitIds(activityText);
    try {
      const res = await retry.mutateAsync({ workflowInstanceIds, activityIds: activityIds.length ? activityIds : undefined });
      setResults(res.results);
      toast.success(`Retry requested for ${workflowInstanceIds.length} instance(s).`);
      onDone?.();
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Retry faulted instances</DialogTitle>
          <DialogDescription>Re-schedules the faulted activities of each instance and resumes it.</DialogDescription>
        </DialogHeader>
        {results ? (
          <>
            <AlterationResults results={results} />
            <DialogFooter>
              <Button variant="outline" onClick={() => setResults(null)}>
                Back
              </Button>
              <Button onClick={() => onOpenChange(false)}>Close</Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <Field label="Workflow instance ids" required htmlFor="retry-instances" hint="One per line, or comma separated.">
              <Textarea id="retry-instances" rows={6} value={instanceText} onChange={(e) => setInstanceText(e.target.value)} className="font-mono text-xs" />
            </Field>
            <Field label="Activity ids" htmlFor="retry-activities" hint="Optional. When empty, every activity with an incident is retried.">
              <Input id="retry-activities" value={activityText} onChange={(e) => setActivityText(e.target.value)} className="font-mono text-xs" />
            </Field>
            <ErrorAlert error={retry.error} />
            <DialogFooter>
              <Button variant="outline" onClick={() => onOpenChange(false)} disabled={retry.isPending}>
                Cancel
              </Button>
              <Button onClick={onRetry} disabled={retry.isPending}>
                {retry.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <RefreshCw data-icon="inline-start" />}
                Retry
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
