"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useState } from "react";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { toast } from "sonner";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { CodeEditor } from "@elsa-workflows/react-studio-ui/common/code-editor";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import type { WorkflowDefinitionSummary } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import { useWorkflowDefinitionMutations } from "../hooks";

/** Runs (dispatches) a published workflow with optional JSON input and a correlation id. */
export function RunWorkflowDialog({ definition, onOpenChange }: { definition: WorkflowDefinitionSummary | null; onOpenChange: (open: boolean) => void }) {
  const _t = useTranslation();
  const [input, setInput] = useState("{}");
  const [correlationId, setCorrelationId] = useState("");
  const { dispatch } = useWorkflowDefinitionMutations();
  const router = useRouter();

  const submit = async () => {
    if (!definition) return;
    let parsed: Record<string, unknown> | undefined;
    try {
      parsed = input.trim() ? (JSON.parse(input) as Record<string, unknown>) : undefined;
    } catch {
      toast.error(_t("Input must be valid JSON."));
      return;
    }
    try {
      const result = await dispatch.mutateAsync({ definitionId: definition.definitionId, input: parsed, correlationId: correlationId || undefined });
      onOpenChange(false);
      toast.success(_t("Workflow started"), {
        description: _t("Instance {{value0}}", { value0: result.workflowInstanceId }),
        action: { label: _t("Open"), onClick: () => router.push(`/workflows/instances/${result.workflowInstanceId}/view`) },
      });
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <Dialog open={!!definition} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{_t("Run")}{" "}{definition?.name ?? _t("workflow")}</DialogTitle>
          <DialogDescription>{_t("Dispatches the published version with the input below.")}</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <Field label={_t("Correlation id")}>
            <Input value={correlationId} onChange={(e) => setCorrelationId(e.target.value)} placeholder={_t("optional")} />
          </Field>
          <Field label={_t("Input (JSON)")}>
            <CodeEditor value={input} onChange={setInput} language="json" height="10rem" />
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {_t("Cancel")}</Button>
          <Button onClick={() => void submit()} disabled={dispatch.isPending}>
            {_t("Run")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
