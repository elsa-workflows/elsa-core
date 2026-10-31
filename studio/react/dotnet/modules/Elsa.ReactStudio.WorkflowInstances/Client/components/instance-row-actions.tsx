"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Ban, Eye, MoreHorizontal, Trash2 } from "lucide-react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@elsa-workflows/react-studio-ui/dropdown-menu";
import type { WorkflowInstanceSummary } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { useExtensions } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
import type { ListActionHelpers } from "@elsa-workflows/react-studio-sdk";
import { usePermissions, Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { WorkflowInstanceRoutes } from "../routes";

export function InstanceRowActions({
  instance,
  helpers,
  onCancel,
  onDelete,
}: {
  instance: WorkflowInstanceSummary;
  helpers: ListActionHelpers;
  onCancel: (instance: WorkflowInstanceSummary) => void;
  onDelete: (instance: WorkflowInstanceSummary) => void;
}) {
  const _t = useTranslation();
  const permissions = usePermissions();
  const canCancel = permissions.has(Resources.workflowInstances, Verbs.cancel);
  const canDelete = permissions.has(Resources.workflowInstances, Verbs.delete);
  const extensions = useExtensions("instanceRowActions").filter(
    (a) => (a.isVisible ? a.isVisible(instance) : true) && permissions.hasAll(a.permissions ?? []),
  );

  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label={_t("Actions")} onClick={(e) => e.stopPropagation()} />}>
        <MoreHorizontal />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44" onClick={(e) => e.stopPropagation()}>
        <DropdownMenuItem render={<Link href={href(WorkflowInstanceRoutes.view, { id: instance.id })} />}>
          <Eye />
          {_t("View")}</DropdownMenuItem>
        {canCancel && (
          <DropdownMenuItem disabled={!!instance.finishedAt} onClick={() => onCancel(instance)}>
            <Ban />
            {_t("Cancel")}</DropdownMenuItem>
        )}
        {extensions.length > 0 && <DropdownMenuSeparator />}
        {extensions.map(({ id, label, icon: Icon, onSelect }) => (
          <DropdownMenuItem key={id} onClick={() => void onSelect(instance, helpers)}>
            {Icon && <Icon />}
            {label}
          </DropdownMenuItem>
        ))}
        {canDelete && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem variant="destructive" onClick={() => onDelete(instance)}>
              <Trash2 />
              {_t("Delete")}</DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
