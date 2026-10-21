"use client";

import Link from "next/link";
import { KeyValueTable } from "@/components/common/key-value-table";
import { StatusBadge } from "@/components/common/status-badge";
import { Timestamp } from "@/components/common/timestamp";
import { CopyButton } from "@/components/common/copy-button";
import type { WorkflowInstance } from "@/lib/api/types/workflow-instances";
import { href } from "@/lib/modules/router";
import { WorkflowDefinitionRoutes } from "@/modules/workflow-definitions";
import { WorkflowInstanceRoutes } from "../routes";

function Mono({ value }: { value: string | undefined }) {
  if (!value) return <span className="text-muted-foreground">–</span>;
  return (
    <span className="inline-flex items-center gap-1">
      <span className="font-mono text-xs break-all">{value}</span>
      <CopyButton value={value} />
    </span>
  );
}

export function InstanceDetails({ instance, definitionName }: { instance: WorkflowInstance; definitionName?: string }) {
  return (
    <KeyValueTable
      className="p-3"
      rows={[
        { label: "ID", value: <Mono value={instance.id} /> },
        {
          label: "Definition",
          value: (
            <Link href={href(WorkflowDefinitionRoutes.edit, { definitionId: instance.definitionId })} className="text-primary hover:underline break-all">
              {definitionName?.trim() || instance.definitionId}
            </Link>
          ),
        },
        { label: "Definition ID", value: <Mono value={instance.definitionId} /> },
        { label: "Version", value: <span className="tabular-nums">{instance.version}</span> },
        { label: "Name", value: instance.name },
        { label: "Correlation ID", value: <Mono value={instance.correlationId} /> },
        {
          label: "Status",
          value: (
            <span className="inline-flex flex-wrap gap-1">
              <StatusBadge status={instance.status} />
              <StatusBadge status={instance.subStatus} />
            </span>
          ),
        },
        { label: "Incidents", value: <span className="tabular-nums">{instance.incidentCount}</span> },
        { label: "Created", value: <Timestamp value={instance.createdAt} /> },
        { label: "Updated", value: <Timestamp value={instance.updatedAt} /> },
        { label: "Finished", value: <Timestamp value={instance.finishedAt} /> },
        {
          label: "Parent instance",
          value: instance.parentWorkflowInstanceId ? (
            <Link href={href(WorkflowInstanceRoutes.view, { id: instance.parentWorkflowInstanceId })} className="font-mono text-xs text-primary hover:underline">
              {instance.parentWorkflowInstanceId}
            </Link>
          ) : undefined,
        },
      ]}
    />
  );
}
