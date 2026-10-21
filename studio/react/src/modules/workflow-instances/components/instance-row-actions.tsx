"use client";

import { Ban, Eye, MoreHorizontal, Trash2 } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { WorkflowInstanceSummary } from "@/lib/api/types/workflow-instances";
import { href } from "@/lib/modules/router";
import { useExtensions } from "@/lib/modules/registry";
import type { ListActionHelpers } from "@/lib/modules/types";
import { usePermissions, Resources, Verbs } from "@/lib/permissions";
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
  const permissions = usePermissions();
  const canCancel = permissions.has(Resources.workflowInstances, Verbs.cancel);
  const canDelete = permissions.has(Resources.workflowInstances, Verbs.delete);
  const extensions = useExtensions("instanceRowActions").filter(
    (a) => (a.isVisible ? a.isVisible(instance) : true) && permissions.hasAll(a.permissions ?? []),
  );

  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label="Actions" onClick={(e) => e.stopPropagation()} />}>
        <MoreHorizontal />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44" onClick={(e) => e.stopPropagation()}>
        <DropdownMenuItem render={<Link href={href(WorkflowInstanceRoutes.view, { id: instance.id })} />}>
          <Eye />
          View
        </DropdownMenuItem>
        {canCancel && (
          <DropdownMenuItem disabled={!!instance.finishedAt} onClick={() => onCancel(instance)}>
            <Ban />
            Cancel
          </DropdownMenuItem>
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
              Delete
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
