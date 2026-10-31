"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Loader2, RefreshCw } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Textarea } from "@elsa-workflows/react-studio-ui/textarea";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { ErrorAlert, describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
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
  const _t = useTranslation();
  const [instanceText, setInstanceText] = useState(initialInstanceIds.join("\n"));
  const [activityText, setActivityText] = useState("");
  const [results, setResults] = useState<RunAlterationsResult[] | null>(null);
  const retry = useRetryWorkflows();

  const onRetry = async () => {
    const workflowInstanceIds = splitIds(instanceText);
    if (workflowInstanceIds.length === 0) {
      toast.error(_t("Enter at least one workflow instance id."));
      return;
    }
    const activityIds = splitIds(activityText);
    try {
      const res = await retry.mutateAsync({ workflowInstanceIds, activityIds: activityIds.length ? activityIds : undefined });
      setResults(res.results);
      toast.success(_t("Retry requested for {{value0}} instance(s).", { value0: workflowInstanceIds.length }));
      onDone?.();
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{_t("Retry faulted instances")}</DialogTitle>
          <DialogDescription>{_t("Re-schedules the faulted activities of each instance and resumes it.")}</DialogDescription>
        </DialogHeader>
        {results ? (
          <>
            <AlterationResults results={results} />
            <DialogFooter>
              <Button variant="outline" onClick={() => setResults(null)}>
                {_t("Back")}</Button>
              <Button onClick={() => onOpenChange(false)}>{_t("Close")}</Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <Field label={_t("Workflow instance ids")} required htmlFor="retry-instances" hint={_t("One per line, or comma separated.")}>
              <Textarea id="retry-instances" rows={6} value={instanceText} onChange={(e) => setInstanceText(e.target.value)} className="font-mono text-xs" />
            </Field>
            <Field label={_t("Activity ids")} htmlFor="retry-activities" hint={_t("Optional. When empty, every activity with an incident is retried.")}>
              <Input id="retry-activities" value={activityText} onChange={(e) => setActivityText(e.target.value)} className="font-mono text-xs" />
            </Field>
            <ErrorAlert error={retry.error} />
            <DialogFooter>
              <Button variant="outline" onClick={() => onOpenChange(false)} disabled={retry.isPending}>
                {_t("Cancel")}</Button>
              <Button onClick={onRetry} disabled={retry.isPending}>
                {retry.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <RefreshCw data-icon="inline-start" />}
                {_t("Retry")}</Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
