"use client";

import { Check, Eye, History, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useConfirm } from "@/components/common/confirm-dialog";
import { describeError } from "@/components/common/error-alert";
import { Timestamp } from "@/components/common/timestamp";
import { Resources, usePermissions, Verbs } from "@/lib/permissions";
import { useWorkflowEditor } from "../editor-context";
import { useWorkflowDefinitionMutations, useWorkflowDefinitionVersions } from "../hooks";

export function VersionsTab({ onView }: { onView: (version: number | undefined) => void }) {
  const { definition } = useWorkflowEditor();
  const versions = useWorkflowDefinitionVersions(definition.definitionId);
  const { revert, removeVersion } = useWorkflowDefinitionMutations();
  const confirm = useConfirm();
  const permissions = usePermissions();
  const canRevert = permissions.has(Resources.workflowDefinitionVersions, Verbs.revert);
  const canDelete = permissions.has(Resources.workflowDefinitions, Verbs.delete);

  const doRevert = async (version: number) => {
    if (!(await confirm({ title: `Roll back to version ${version}?`, description: "A new draft version is created with the contents of the selected version." }))) return;
    try {
      await revert.mutateAsync({ definitionId: definition.definitionId, version });
      toast.success(`Rolled back to version ${version}`);
      onView(undefined);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const doDelete = async (id: string, version: number) => {
    if (!(await confirm({ title: `Delete version ${version}?`, destructive: true, confirmLabel: "Delete" }))) return;
    try {
      await removeVersion.mutateAsync(id);
      toast.success(`Version ${version} deleted`);
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
              <TableHead>Version</TableHead>
              <TableHead>Created</TableHead>
              <TableHead className="w-28" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {(versions.data ?? []).map((v) => (
              <TableRow key={v.id} data-state={v.version === definition.version ? "selected" : undefined}>
                <TableCell>{v.isPublished && <Tooltip><TooltipTrigger render={<span />}><Check className="size-4 text-emerald-600" /></TooltipTrigger><TooltipContent>Published</TooltipContent></Tooltip>}</TableCell>
                <TableCell className="font-medium">
                  v{v.version} {v.isLatest && <span className="text-xs text-muted-foreground">(latest)</span>}
                </TableCell>
                <TableCell><Timestamp value={v.createdAt} /></TableCell>
                <TableCell>
                  <div className="flex justify-end gap-0.5">
                    <Button variant="ghost" size="icon-xs" aria-label="View" onClick={() => onView(v.isLatest ? undefined : v.version)}><Eye /></Button>
                    {canRevert && !v.isLatest && <Button variant="ghost" size="icon-xs" aria-label="Roll back" onClick={() => void doRevert(v.version)}><History /></Button>}
                    {canDelete && !v.isLatest && !v.isPublished && <Button variant="ghost" size="icon-xs" aria-label="Delete" onClick={() => void doDelete(v.id, v.version)}><Trash2 /></Button>}
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
