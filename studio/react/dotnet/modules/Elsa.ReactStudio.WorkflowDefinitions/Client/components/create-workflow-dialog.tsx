"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useState } from "react";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { toast } from "sonner";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Textarea } from "@elsa-workflows/react-studio-ui/textarea";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { emptyContainer, FLOW_TYPES } from "@elsa-workflows/react-studio-sdk/designer/index";
import { cn } from "@elsa-workflows/react-studio-ui/utils";

/** Shared with the branch-slot chooser, so picking a flow reads the same in both places. */
const ROOT_TYPES = FLOW_TYPES;
import { useWorkflowDefinitionMutations } from "../hooks";
import { WorkflowDefinitionRoutes } from "../routes";

/** Creates a new definition with an empty flowchart and opens it in the editor. */
export function CreateWorkflowDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const _t = useTranslation();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [rootType, setRootType] = useState<string>(ROOT_TYPES[0].type);
  const { save } = useWorkflowDefinitionMutations();
  const router = useRouter();

  const submit = async () => {
    try {
      const flowchart = emptyContainer(rootType)!;
      const result = await save.mutateAsync({
        model: {
          name: name.trim(),
          description: description.trim() || undefined,
          variables: [],
          inputs: [],
          outputs: [],
          outcomes: [],
          customProperties: {},
          options: {},
          root: { ...flowchart, nodeId: `Workflow1:${flowchart.id}` },
        },
        publish: false,
      });
      onOpenChange(false);
      setName("");
      setDescription("");
      router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: result.workflowDefinition.definitionId }));
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{_t("Create workflow")}</DialogTitle>
          <DialogDescription>{_t("Give the workflow a name. You can change everything later.")}</DialogDescription>
        </DialogHeader>
        <form
          className="flex flex-col gap-4"
          onSubmit={(e) => {
            e.preventDefault();
            void submit();
          }}
        >
          <Field label={_t("Name")} required htmlFor="wf-name">
            <Input id="wf-name" value={name} onChange={(e) => setName(e.target.value)} autoFocus required />
          </Field>
          <Field label={_t("Description")} htmlFor="wf-desc">
            <Textarea id="wf-desc" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
          </Field>
          <Field label={_t("Designer")}>
            <div className="grid grid-cols-3 gap-2" role="radiogroup">
              {ROOT_TYPES.map((r) => (
                <button
                  key={r.type}
                  type="button"
                  role="radio"
                  aria-checked={rootType === r.type}
                  onClick={() => setRootType(r.type)}
                  className={cn("flex flex-col items-start gap-1 rounded-lg border p-2.5 text-left transition-colors hover:bg-accent", rootType === r.type ? "border-primary bg-accent" : "border-border")}
                >
                  <r.icon className={cn("size-4", rootType === r.type ? "text-primary" : "text-muted-foreground")} />
                  <span className="text-sm font-medium">{_t(r.label)}</span>
                  <span className="text-[11px] leading-snug text-muted-foreground">{_t(r.description)}</span>
                </button>
              ))}
            </div>
          </Field>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              {_t("Cancel")}</Button>
            <Button type="submit" disabled={!name.trim() || save.isPending}>
              {_t("Create")}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
