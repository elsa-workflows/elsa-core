"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { KeyValueTable } from "@elsa-workflows/react-studio-ui/common/key-value-table";
import { StatusBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { CopyButton } from "@elsa-workflows/react-studio-ui/common/copy-button";
import type { WorkflowInstance } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { WorkflowDefinitionRoutes } from "@elsa-workflows/react-studio-sdk/routes";
import { WorkflowInstanceRoutes } from "../routes";

function Mono({ value }: { value: string | undefined }) {
  useTranslation();
  if (!value) return <span className="text-muted-foreground">–</span>;
  return (
    <span className="inline-flex items-center gap-1">
      <span className="font-mono text-xs break-all">{value}</span>
      <CopyButton value={value} />
    </span>
  );
}

export function InstanceDetails({ instance, definitionName }: { instance: WorkflowInstance; definitionName?: string }) {
  const _t = useTranslation();
  return (
    <KeyValueTable
      className="p-3"
      rows={[
        { label: _t("ID"), value: <Mono value={instance.id} /> },
        {
          label: _t("Definition"),
          value: (
            <Link href={href(WorkflowDefinitionRoutes.edit, { definitionId: instance.definitionId })} className="text-primary hover:underline break-all">
              {definitionName?.trim() || instance.definitionId}
            </Link>
          ),
        },
        { label: _t("Definition ID"), value: <Mono value={instance.definitionId} /> },
        { label: _t("Version"), value: <span className="tabular-nums">{instance.version}</span> },
        { label: _t("Name"), value: instance.name },
        { label: _t("Correlation ID"), value: <Mono value={instance.correlationId} /> },
        {
          label: _t("Status"),
          value: (
            <span className="inline-flex flex-wrap gap-1">
              <StatusBadge status={instance.status} />
              <StatusBadge status={instance.subStatus} />
            </span>
          ),
        },
        { label: _t("Incidents"), value: <span className="tabular-nums">{instance.incidentCount}</span> },
        { label: _t("Created"), value: <Timestamp value={instance.createdAt} /> },
        { label: _t("Updated"), value: <Timestamp value={instance.updatedAt} /> },
        { label: _t("Finished"), value: <Timestamp value={instance.finishedAt} /> },
        {
          label: _t("Parent instance"),
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
