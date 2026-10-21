"use client";

import { Loader2, Play, Send } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/common/field";
import { ErrorAlert, describeError } from "@/components/common/error-alert";
import { useConfirm } from "@/components/common/confirm-dialog";
import { href } from "@/lib/modules/router";
import type { RunAlterationsResult } from "../api";
import { useRetryWorkflows, useRunAlterations, useSubmitAlterationPlan } from "../hooks";
import type { AlterTarget } from "../dialog-store";
import { AlterationComposer, newDraft, toAlterations, type AlterationDraft } from "./alteration-composer";
import { AlterationResults } from "./alteration-results";
import { AlterationRoutes } from "../routes";

type Outcome = { kind: "run"; results: RunAlterationsResult[] } | { kind: "submitted"; planId: string };

/**
 * Compose alterations for one or more workflow instances and either run them immediately
 * (`POST /alterations/run`) or submit them as a plan (`POST /alterations/submit`).
 */
export function AlterInstanceDialog({
  targets,
  open,
  onOpenChange,
  onDone,
}: {
  targets: AlterTarget[];
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onDone?: () => void;
}) {
  const [drafts, setDrafts] = useState<AlterationDraft[]>(() => [newDraft()]);
  const [retryActivityIds, setRetryActivityIds] = useState(() => targets.flatMap((t) => t.faultedActivityIds ?? []).join(", "));
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const [validation, setValidation] = useState<string | undefined>();
  const confirm = useConfirm();

  const run = useRunAlterations();
  const submit = useSubmitAlterationPlan();
  const retry = useRetryWorkflows();
  const busy = run.isPending || submit.isPending || retry.isPending;
  const instanceIds = targets.map((t) => t.workflowInstanceId);
  const error = run.error ?? submit.error ?? retry.error;

  const prepared = () => {
    const { alterations, error } = toAlterations(drafts);
    setValidation(error);
    return error ? undefined : alterations;
  };

  const onRun = async () => {
    const alterations = prepared();
    if (!alterations) return;
    if (!(await confirm({ title: "Run alterations now?", description: `${alterations.length} alteration(s) will be applied immediately to ${instanceIds.length} instance(s).`, confirmLabel: "Run" }))) return;
    try {
      const res = await run.mutateAsync({ alterations, workflowInstanceIds: instanceIds });
      setOutcome({ kind: "run", results: res.results });
      const failed = res.results.filter((r) => !r.isSuccessful).length;
      if (failed) toast.warning(`${failed} of ${res.results.length} instance(s) reported errors.`);
      else toast.success(`Alterations applied to ${res.results.length} instance(s).`);
      onDone?.();
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const onSubmit = async () => {
    const alterations = prepared();
    if (!alterations) return;
    try {
      const res = await submit.mutateAsync({ alterations, filter: { workflowInstanceIds: instanceIds } });
      setOutcome({ kind: "submitted", planId: res.planId });
      toast.success("Alteration plan submitted.");
      onDone?.();
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const onRetry = async () => {
    const activityIds = retryActivityIds.split(/[\s,]+/).map((s) => s.trim()).filter(Boolean);
    try {
      const res = await retry.mutateAsync({ workflowInstanceIds: instanceIds, activityIds: activityIds.length ? activityIds : undefined });
      setOutcome({ kind: "run", results: res.results });
      toast.success(`Retry requested for ${instanceIds.length} instance(s).`);
      onDone?.();
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Alter workflow instance{targets.length > 1 ? "s" : ""}</DialogTitle>
          <DialogDescription>
            {targets.length === 1 ? (
              <>
                <span className="font-mono text-xs">{targets[0].workflowInstanceId}</span>
                {targets[0].name && <> · {targets[0].name}</>}
                {targets[0].version !== undefined && <> · v{targets[0].version}</>}
              </>
            ) : (
              <>{targets.length} instances selected.</>
            )}
          </DialogDescription>
        </DialogHeader>

        {outcome ? (
          <div className="flex flex-col gap-3">
            {outcome.kind === "run" ? (
              <AlterationResults results={outcome.results} />
            ) : (
              <p className="text-sm">
                Plan <span className="font-mono text-xs">{outcome.planId}</span> was submitted and is being processed in the background.{" "}
                <Link className="underline underline-offset-4" href={href(AlterationRoutes.plan, { id: outcome.planId })} onClick={() => onOpenChange(false)}>
                  Open the plan
                </Link>{" "}
                to follow its jobs.
              </p>
            )}
            <DialogFooter>
              <Button variant="outline" onClick={() => setOutcome(null)}>
                Back
              </Button>
              <Button onClick={() => onOpenChange(false)}>Close</Button>
            </DialogFooter>
          </div>
        ) : (
          <Tabs defaultValue="compose">
            <TabsList>
              <TabsTrigger value="compose">Compose alterations</TabsTrigger>
              <TabsTrigger value="retry">Retry faulted activities</TabsTrigger>
            </TabsList>
            <TabsContent value="compose" className="flex flex-col gap-3 pt-2">
              <AlterationComposer drafts={drafts} onChange={setDrafts} />
              {validation && <p className="text-xs text-destructive">{validation}</p>}
              <ErrorAlert error={error} />
              <DialogFooter>
                <Button variant="outline" onClick={() => onOpenChange(false)} disabled={busy}>
                  Cancel
                </Button>
                <Button variant="secondary" onClick={onSubmit} disabled={busy}>
                  {submit.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Send data-icon="inline-start" />}
                  Submit as plan
                </Button>
                <Button onClick={onRun} disabled={busy}>
                  {run.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Play data-icon="inline-start" />}
                  Run now
                </Button>
              </DialogFooter>
            </TabsContent>
            <TabsContent value="retry" className="flex flex-col gap-3 pt-2">
              <p className="text-sm text-muted-foreground">
                Re-schedules faulted activities (`POST /alterations/workflows/retry`). Leave the list empty to retry every activity that raised an incident.
              </p>
              <Field label="Activity ids" htmlFor="retry-activity-ids" hint="Comma or whitespace separated. Optional.">
                <Input id="retry-activity-ids" value={retryActivityIds} onChange={(e) => setRetryActivityIds(e.target.value)} className="font-mono text-xs" />
              </Field>
              <ErrorAlert error={retry.error} />
              <DialogFooter>
                <Button variant="outline" onClick={() => onOpenChange(false)} disabled={busy}>
                  Cancel
                </Button>
                <Button onClick={onRetry} disabled={busy}>
                  {retry.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Play data-icon="inline-start" />}
                  Retry
                </Button>
              </DialogFooter>
            </TabsContent>
          </Tabs>
        )}
      </DialogContent>
    </Dialog>
  );
}
