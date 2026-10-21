"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Check, Eye, History, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@elsa-workflows/react-studio-ui/table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@elsa-workflows/react-studio-ui/tooltip";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { Resources, usePermissions, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { useWorkflowEditor } from "../editor-context";
import { useWorkflowDefinitionMutations, useWorkflowDefinitionVersions } from "../hooks";

export function VersionsTab({ onView }: { onView: (version: number | undefined) => void }) {
  const _t = useTranslation();
  const { definition } = useWorkflowEditor();
  const versions = useWorkflowDefinitionVersions(definition.definitionId);
  const { revert, removeVersion } = useWorkflowDefinitionMutations();
  const confirm = useConfirm();
  const permissions = usePermissions();
  const canRevert = permissions.has(Resources.workflowDefinitionVersions, Verbs.revert);
  const canDelete = permissions.has(Resources.workflowDefinitions, Verbs.delete);

  const doRevert = async (version: number) => {
    if (!(await confirm({ title: _t("Roll back to version {{value0}}?", { value0: version }), description: _t("A new draft version is created with the contents of the selected version.") }))) return;
    try {
      await revert.mutateAsync({ definitionId: definition.definitionId, version });
      toast.success(_t("Rolled back to version {{value0}}", { value0: version }));
      onView(undefined);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const doDelete = async (id: string, version: number) => {
    if (!(await confirm({ title: _t("Delete version {{value0}}?", { value0: version }), destructive: true, confirmLabel: _t("Delete") }))) return;
    try {
      await removeVersion.mutateAsync(id);
      toast.success(_t("Version {{value0}} deleted", { value0: version }));
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  return (
    <div className="p-3">
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-10" />
              <TableHead>{_t("Version")}</TableHead>
              <TableHead>{_t("Created")}</TableHead>
              <TableHead className="w-28" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {(versions.data ?? []).map((v) => (
              <TableRow key={v.id} data-state={v.version === definition.version ? "selected" : undefined}>
                <TableCell>{v.isPublished && <Tooltip><TooltipTrigger render={<span />}><Check className="size-4 text-emerald-600" /></TooltipTrigger><TooltipContent>{_t("Published")}</TooltipContent></Tooltip>}</TableCell>
                <TableCell className="font-medium">
                  {_t("v")}{v.version} {v.isLatest && <span className="text-xs text-muted-foreground">{_t("(latest)")}</span>}
                </TableCell>
                <TableCell><Timestamp value={v.createdAt} /></TableCell>
                <TableCell>
                  <div className="flex justify-end gap-0.5">
                    <Button variant="ghost" size="icon-xs" aria-label={_t("View")} onClick={() => onView(v.isLatest ? undefined : v.version)}><Eye /></Button>
                    {canRevert && !v.isLatest && <Button variant="ghost" size="icon-xs" aria-label={_t("Roll back")} onClick={() => void doRevert(v.version)}><History /></Button>}
                    {canDelete && !v.isLatest && !v.isPublished && <Button variant="ghost" size="icon-xs" aria-label={_t("Delete")} onClick={() => void doDelete(v.id, v.version)}><Trash2 /></Button>}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
