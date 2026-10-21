"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Loader2, Play, Send } from "lucide-react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@elsa-workflows/react-studio-ui/tabs";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { ErrorAlert, describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
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
  const _t = useTranslation();
  const [drafts, setDrafts] = useState<AlterationDraft[]>(() => [newDraft()]);
  const [retryActivityIds, setRetryActivityIds] = useState(() => targets.flatMap((t) => t.faultedActivityIds ?? []).join(", "));
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  // Retain the rejected draft, not a message translated in the previous culture.
  const [validation, setValidation] = useState<AlterationDraft[] | undefined>();
  const confirm = useConfirm();

  const run = useRunAlterations();
  const submit = useSubmitAlterationPlan();
  const retry = useRetryWorkflows();
  const busy = run.isPending || submit.isPending || retry.isPending;
  const instanceIds = targets.map((t) => t.workflowInstanceId);
  const error = run.error ?? submit.error ?? retry.error;

  const prepared = () => {
    const { alterations, error } = toAlterations(drafts);
    setValidation(error ? drafts : undefined);
    return error ? undefined : alterations;
  };

  const onRun = async () => {
    const alterations = prepared();
    if (!alterations) return;
    if (!(await confirm({ title: _t("Run alterations now?"), description: _t("{{value0}} alteration(s) will be applied immediately to {{value1}} instance(s).", { value0: alterations.length, value1: instanceIds.length }), confirmLabel: _t("Run") }))) return;
    try {
      const res = await run.mutateAsync({ alterations, workflowInstanceIds: instanceIds });
      setOutcome({ kind: "run", results: res.results });
      const failed = res.results.filter((r) => !r.isSuccessful).length;
      if (failed) toast.warning(_t("{{value0}} of {{value1}} instance(s) reported errors.", { value0: failed, value1: res.results.length }));
      else toast.success(_t("Alterations applied to {{value0}} instance(s).", { value0: res.results.length }));
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
      toast.success(_t("Alteration plan submitted."));
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
      toast.success(_t("Retry requested for {{value0}} instance(s).", { value0: instanceIds.length }));
      onDone?.();
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{_t("Alter workflow instance")}{targets.length > 1 ? _t("s") : ""}</DialogTitle>
          <DialogDescription>
            {targets.length === 1 ? (
              <>
                <span className="font-mono text-xs">{targets[0].workflowInstanceId}</span>
                {targets[0].name && <> · {targets[0].name}</>}
                {targets[0].version !== undefined && <> {_t("· v")}{targets[0].version}</>}
              </>
            ) : (
              <>{targets.length} {_t("instances selected.")}</>
            )}
          </DialogDescription>
        </DialogHeader>

        {outcome ? (
          <div className="flex flex-col gap-3">
            {outcome.kind === "run" ? (
              <AlterationResults results={outcome.results} />
            ) : (
              <p className="text-sm">
                {_t("Plan")}{" "}<span className="font-mono text-xs">{outcome.planId}</span> {_t("was submitted and is being processed in the background.")}{" "}
                <Link className="underline underline-offset-4" href={href(AlterationRoutes.plan, { id: outcome.planId })} onClick={() => onOpenChange(false)}>
                  {_t("Open the plan")}</Link>{" "}
                {_t("to follow its jobs.")}</p>
            )}
            <DialogFooter>
              <Button variant="outline" onClick={() => setOutcome(null)}>
                {_t("Back")}</Button>
              <Button onClick={() => onOpenChange(false)}>{_t("Close")}</Button>
            </DialogFooter>
          </div>
        ) : (
          <Tabs defaultValue="compose">
            <TabsList>
              <TabsTrigger value="compose">{_t("Compose alterations")}</TabsTrigger>
              <TabsTrigger value="retry">{_t("Retry faulted activities")}</TabsTrigger>
            </TabsList>
            <TabsContent value="compose" className="flex flex-col gap-3 pt-2">
              <AlterationComposer drafts={drafts} onChange={setDrafts} />
              {validation && <p className="text-xs text-destructive">{toAlterations(validation).error}</p>}
              <ErrorAlert error={error} />
              <DialogFooter>
                <Button variant="outline" onClick={() => onOpenChange(false)} disabled={busy}>
                  {_t("Cancel")}</Button>
                <Button variant="secondary" onClick={onSubmit} disabled={busy}>
                  {submit.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Send data-icon="inline-start" />}
                  {_t("Submit as plan")}</Button>
                <Button onClick={onRun} disabled={busy}>
                  {run.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Play data-icon="inline-start" />}
                  {_t("Run now")}</Button>
              </DialogFooter>
            </TabsContent>
            <TabsContent value="retry" className="flex flex-col gap-3 pt-2">
              <p className="text-sm text-muted-foreground">
                {_t("Re-schedules faulted activities (`POST /alterations/workflows/retry`). Leave the list empty to retry every activity that raised an incident.")}</p>
              <Field label={_t("Activity ids")} htmlFor="retry-activity-ids" hint={_t("Comma or whitespace separated. Optional.")}>
                <Input id="retry-activity-ids" value={retryActivityIds} onChange={(e) => setRetryActivityIds(e.target.value)} className="font-mono text-xs" />
              </Field>
              <ErrorAlert error={retry.error} />
              <DialogFooter>
                <Button variant="outline" onClick={() => onOpenChange(false)} disabled={busy}>
                  {_t("Cancel")}</Button>
                <Button onClick={onRetry} disabled={busy}>
                  {retry.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Play data-icon="inline-start" />}
                  {_t("Retry")}</Button>
              </DialogFooter>
            </TabsContent>
          </Tabs>
        )}
      </DialogContent>
    </Dialog>
  );
}
