"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Loader2 } from "lucide-react";
import { EmptyState } from "@elsa-workflows/react-studio-ui/common/empty-state";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@elsa-workflows/react-studio-ui/table";
import type { WorkflowInstanceVariable } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";

function VariableValue({ value }: { value: unknown }) {
  const _t = useTranslation();
  if (value === null || value === undefined) return <span className="text-muted-foreground">{_t("null")}</span>;
  if (typeof value === "object") return <JsonViewer value={value} maxHeight="12rem" />;
  return <span className="break-all font-mono text-xs">{String(value)}</span>;
}

export function VariablesPanel({ variables, isLoading, error }: { variables: WorkflowInstanceVariable[]; isLoading: boolean; error: unknown }) {
  const _t = useTranslation();
  if (isLoading)
    return (
      <div className="flex items-center gap-2 p-4 text-sm text-muted-foreground">
        <Loader2 className="size-4 animate-spin" /> {_t("Loading variables…")}</div>
    );
  if (error)
    return (
      <div className="p-3">
        <ErrorAlert error={error} title={_t("Could not load variables")} />
      </div>
    );
  if (variables.length === 0) return <EmptyState title={_t("No variables")} className="m-3 p-6" />;
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-40">{_t("Name")}</TableHead>
          <TableHead>{_t("Value")}</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {variables.map((v) => (
          <TableRow key={v.id}>
            <TableCell className="align-top font-medium">{v.name}</TableCell>
            <TableCell className="align-top">
              <VariableValue value={v.value} />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
