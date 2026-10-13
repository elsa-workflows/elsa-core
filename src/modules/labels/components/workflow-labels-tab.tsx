"use client";

import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { Checkbox } from "@/components/ui/checkbox";
import { Label as FormLabel } from "@/components/ui/label";
import { Resources, usePermission, Verbs } from "@/lib/permissions";
import { useWorkflowEditor } from "@/modules/workflow-definitions/editor-context";
import { useAllLabels, useDefinitionLabels, useSetDefinitionLabels } from "../hooks";
import { LabelSwatch } from "./label-badge";

/**
 * Properties-pane tab that toggles the labels applied to the definition being edited. Labels are
 * stored per definition *version* on the server, so the version id (`definition.id`) is the key.
 */
export function WorkflowLabelsTab() {
  const { definition, readOnly } = useWorkflowEditor();
  const versionId = definition.id || undefined;
  const canUpdate = usePermission(Resources.workflowDefinitionLabels, Verbs.update) && !readOnly;
  const all = useAllLabels();
  const applied = useDefinitionLabels(versionId);
  const setLabels = useSetDefinitionLabels(versionId);

  if (!versionId) {
    return <p className="p-3 text-sm text-muted-foreground">Save the workflow once before applying labels.</p>;
  }

  const appliedIds = new Set(applied.data ?? []);

  const toggle = async (id: string, checked: boolean) => {
    const next = new Set(appliedIds);
    if (checked) next.add(id);
    else next.delete(id);
    try {
      await setLabels.mutateAsync([...next]);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <div className="flex flex-col gap-3 p-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted-foreground">
          {appliedIds.size} of {all.data?.length ?? 0} label(s) applied to version {definition.version}.
        </p>
        {(setLabels.isPending || applied.isFetching) && <Loader2 className="size-3.5 animate-spin text-muted-foreground" />}
      </div>
      <ErrorAlert error={all.error ?? applied.error} title="Labels could not be loaded" />
      {all.isLoading ? (
        <p className="text-sm text-muted-foreground">Loading labels…</p>
      ) : (all.data?.length ?? 0) === 0 ? (
        <p className="text-sm text-muted-foreground">No labels exist yet. Create some on the Labels page first.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {all.data?.map((label) => {
            const id = `wf-label-${label.id}`;
            return (
              <li key={label.id} className="flex items-start gap-2">
                <Checkbox
                  id={id}
                  checked={appliedIds.has(label.id)}
                  onCheckedChange={(c) => void toggle(label.id, c === true)}
                  disabled={!canUpdate || setLabels.isPending}
                  className="mt-0.5"
                />
                <FormLabel htmlFor={id} className="font-normal flex-col items-start gap-0.5">
                  <span className="flex items-center gap-1.5">
                    <LabelSwatch color={label.color} />
                    {label.name}
                  </span>
                  {label.description && <span className="text-xs text-muted-foreground">{label.description}</span>}
                </FormLabel>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
