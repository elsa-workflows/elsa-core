"use client";

import { Pencil, Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/common/confirm-dialog";
import { DataTable, type DataTableColumn } from "@/components/common/data-table";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { useStudioConfig } from "@/config/provider";
import { Resources, usePermission, Verbs } from "@/lib/permissions";
import type { Label } from "../api";
import { useDeleteLabel, useLabels } from "../hooks";
import { LabelDialog } from "../components/label-dialog";
import { LabelSwatch } from "../components/label-badge";

export function LabelsPage() {
  const config = useStudioConfig();
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(config.ui.defaultPageSize);
  const labels = useLabels(page, pageSize);
  const remove = useDeleteLabel();
  const confirm = useConfirm();
  const canCreate = usePermission(Resources.labels, Verbs.create);
  const canUpdate = usePermission(Resources.labels, Verbs.update);
  const canDelete = usePermission(Resources.labels, Verbs.delete);
  const [dialog, setDialog] = useState<{ open: boolean; label?: Label }>({ open: false });

  const onDelete = async (label: Label) => {
    const ok = await confirm({
      title: `Delete label "${label.name}"?`,
      description: "The label is removed from every workflow definition it is applied to.",
      confirmLabel: "Delete",
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(label.id);
      toast.success(`Label "${label.name}" deleted`);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<Label>[] = [
    { id: "color", header: "", width: "2.5rem", cell: (l) => <LabelSwatch color={l.color} className="size-4" /> },
    { id: "name", header: "Name", cell: (l) => <span className="font-medium">{l.name}</span> },
    { id: "description", header: "Description", cell: (l) => l.description || <span className="text-muted-foreground">–</span> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-24",
      className: "text-right",
      cell: (l) => (
        <div className="flex justify-end gap-1">
          {canUpdate && (
            <Button variant="ghost" size="icon-sm" aria-label={`Edit ${l.name}`} onClick={() => setDialog({ open: true, label: l })}>
              <Pencil />
            </Button>
          )}
          {canDelete && (
            <Button variant="ghost" size="icon-sm" aria-label={`Delete ${l.name}`} onClick={() => void onDelete(l)}>
              <Trash2 />
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Labels"
        description="Tags you can apply to workflow definitions."
        actions={
          canCreate && (
            <Button onClick={() => setDialog({ open: true })}>
              <Plus />
              Create label
            </Button>
          )
        }
      />
      <ErrorAlert error={labels.error} title="Labels could not be loaded" />
      <DataTable
        columns={columns}
        rows={labels.data?.items ?? []}
        rowKey={(l) => l.id}
        isLoading={labels.isLoading}
        emptyMessage="No labels yet."
        paging={{
          page,
          pageSize,
          totalCount: labels.data?.totalCount ?? 0,
          onChange: (p, size) => {
            setPage(p);
            setPageSize(size);
          },
        }}
      />
      <LabelDialog open={dialog.open} onOpenChange={(open) => setDialog((d) => ({ ...d, open }))} label={dialog.label} />
    </div>
  );
}
