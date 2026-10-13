"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Field } from "@/components/common/field";
import { describeError } from "@/components/common/error-alert";
import { href } from "@/lib/modules/router";
import { emptyContainer } from "@/modules/designer";
import { cn } from "@/lib/utils";
import { GitBranch, ListOrdered, Workflow } from "lucide-react";

const ROOT_TYPES = [
  { type: "Elsa.Flowchart", label: "Flowchart", description: "Free-form graph of steps joined by outcomes.", icon: Workflow },
  { type: "Elsa.Sequence", label: "Sequence", description: "Steps that run one after another.", icon: ListOrdered },
  { type: "Elsa.StateMachine", label: "State machine", description: "Named states with trigger-driven transitions.", icon: GitBranch },
] as const;
import { useWorkflowDefinitionMutations } from "../hooks";
import { WorkflowDefinitionRoutes } from "../routes";

/** Creates a new definition with an empty flowchart and opens it in the editor. */
export function CreateWorkflowDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [rootType, setRootType] = useState<(typeof ROOT_TYPES)[number]["type"]>("Elsa.Flowchart");
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
          <DialogTitle>Create workflow</DialogTitle>
          <DialogDescription>Give the workflow a name. You can change everything later.</DialogDescription>
        </DialogHeader>
        <form
          className="flex flex-col gap-4"
          onSubmit={(e) => {
            e.preventDefault();
            void submit();
          }}
        >
          <Field label="Name" required htmlFor="wf-name">
            <Input id="wf-name" value={name} onChange={(e) => setName(e.target.value)} autoFocus required />
          </Field>
          <Field label="Description" htmlFor="wf-desc">
            <Textarea id="wf-desc" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
          </Field>
          <Field label="Designer">
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
                  <span className="text-sm font-medium">{r.label}</span>
                  <span className="text-[11px] leading-snug text-muted-foreground">{r.description}</span>
                </button>
              ))}
            </div>
          </Field>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={!name.trim() || save.isPending}>
              Create
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
