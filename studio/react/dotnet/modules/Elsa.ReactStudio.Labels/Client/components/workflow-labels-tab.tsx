"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { describeError, ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { Checkbox } from "@elsa-workflows/react-studio-ui/checkbox";
import { Label as FormLabel } from "@elsa-workflows/react-studio-ui/label";
import { Resources, usePermission, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { useWorkflowEditor } from "@elsa-workflows/react-studio-sdk";
import { useAllLabels, useDefinitionLabels, useSetDefinitionLabels } from "../hooks";
import { LabelSwatch } from "./label-badge";

/**
 * Properties-pane tab that toggles the labels applied to the definition being edited. Labels are
 * stored per definition *version* on the server, so the version id (`definition.id`) is the key.
 */
export function WorkflowLabelsTab() {
  const _t = useTranslation();
  const { definition, readOnly } = useWorkflowEditor();
  const versionId = definition.id || undefined;
  const canUpdate = usePermission(Resources.workflowDefinitionLabels, Verbs.update) && !readOnly;
  const all = useAllLabels();
  const applied = useDefinitionLabels(versionId);
  const setLabels = useSetDefinitionLabels(versionId);

  if (!versionId) {
    return <p className="p-3 text-sm text-muted-foreground">{_t("Save the workflow once before applying labels.")}</p>;
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
          {appliedIds.size} {_t("of")}{" "}{all.data?.length ?? 0} {_t("label(s) applied to version")}{" "}{definition.version}.
        </p>
        {(setLabels.isPending || applied.isFetching) && <Loader2 className="size-3.5 animate-spin text-muted-foreground" />}
      </div>
      <ErrorAlert error={all.error ?? applied.error} title={_t("Labels could not be loaded")} />
      {all.isLoading ? (
        <p className="text-sm text-muted-foreground">{_t("Loading labels…")}</p>
      ) : (all.data?.length ?? 0) === 0 ? (
        <p className="text-sm text-muted-foreground">{_t("No labels exist yet. Create some on the Labels page first.")}</p>
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
