"use client";

import { Loader2 } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { ErrorAlert } from "@/components/common/error-alert";
import { JsonViewer } from "@/components/common/json-viewer";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { WorkflowInstanceVariable } from "@/lib/api/types/workflow-instances";

function VariableValue({ value }: { value: unknown }) {
  if (value === null || value === undefined) return <span className="text-muted-foreground">null</span>;
  if (typeof value === "object") return <JsonViewer value={value} maxHeight="12rem" />;
  return <span className="break-all font-mono text-xs">{String(value)}</span>;
}

export function VariablesPanel({ variables, isLoading, error }: { variables: WorkflowInstanceVariable[]; isLoading: boolean; error: unknown }) {
  if (isLoading)
    return (
      <div className="flex items-center gap-2 p-4 text-sm text-muted-foreground">
        <Loader2 className="size-4 animate-spin" /> Loading variables…
      </div>
    );
  if (error)
    return (
      <div className="p-3">
        <ErrorAlert error={error} title="Could not load variables" />
      </div>
    );
  if (variables.length === 0) return <EmptyState title="No variables" className="m-3 p-6" />;
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-40">Name</TableHead>
          <TableHead>Value</TableHead>
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
