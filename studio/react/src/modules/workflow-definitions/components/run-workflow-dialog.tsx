"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { CodeEditor } from "@/components/common/code-editor";
import { Field } from "@/components/common/field";
import { describeError } from "@/components/common/error-alert";
import type { WorkflowDefinitionSummary } from "@/lib/api/types/workflow-definitions";
import { useWorkflowDefinitionMutations } from "../hooks";

/** Runs (dispatches) a published workflow with optional JSON input and a correlation id. */
export function RunWorkflowDialog({ definition, onOpenChange }: { definition: WorkflowDefinitionSummary | null; onOpenChange: (open: boolean) => void }) {
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
      toast.error("Input must be valid JSON.");
      return;
    }
    try {
      const result = await dispatch.mutateAsync({ definitionId: definition.definitionId, input: parsed, correlationId: correlationId || undefined });
      onOpenChange(false);
      toast.success("Workflow started", {
        description: `Instance ${result.workflowInstanceId}`,
        action: { label: "Open", onClick: () => router.push(`/workflows/instances/${result.workflowInstanceId}/view`) },
      });
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <Dialog open={!!definition} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Run {definition?.name ?? "workflow"}</DialogTitle>
          <DialogDescription>Dispatches the published version with the input below.</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <Field label="Correlation id">
            <Input value={correlationId} onChange={(e) => setCorrelationId(e.target.value)} placeholder="optional" />
          </Field>
          <Field label="Input (JSON)">
            <CodeEditor value={input} onChange={setInput} language="json" height="10rem" />
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={() => void submit()} disabled={dispatch.isPending}>
            Run
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
