"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Pencil, Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { DataTable, type DataTableColumn } from "@elsa-workflows/react-studio-ui/common/data-table";
import { describeError, ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { useStudioConfig } from "@elsa-workflows/react-studio-sdk/config-provider";
import { Resources, usePermission, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import type { Label } from "../api";
import { useDeleteLabel, useLabels } from "../hooks";
import { LabelDialog } from "../components/label-dialog";
import { LabelSwatch } from "../components/label-badge";

export function LabelsPage() {
  const _t = useTranslation();
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
      title: _t("Delete label \"{{value0}}\"?", { value0: label.name }),
      description: _t("The label is removed from every workflow definition it is applied to."),
      confirmLabel: _t("Delete"),
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(label.id);
      toast.success(_t("Label \"{{value0}}\" deleted", { value0: label.name }));
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<Label>[] = [
    { id: "color", header: "", width: "2.5rem", cell: (l) => <LabelSwatch color={l.color} className="size-4" /> },
    { id: "name", header: _t("Name"), cell: (l) => <span className="font-medium">{l.name}</span> },
    { id: "description", header: _t("Description"), cell: (l) => l.description || <span className="text-muted-foreground">–</span> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-24",
      className: "text-right",
      cell: (l) => (
        <div className="flex justify-end gap-1">
          {canUpdate && (
            <Button variant="ghost" size="icon-sm" aria-label={_t("Edit {{value0}}", { value0: l.name })} onClick={() => setDialog({ open: true, label: l })}>
              <Pencil />
            </Button>
          )}
          {canDelete && (
            <Button variant="ghost" size="icon-sm" aria-label={_t("Delete {{value0}}", { value0: l.name })} onClick={() => void onDelete(l)}>
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
        title={_t("Labels")}
        description={_t("Tags you can apply to workflow definitions.")}
        actions={
          canCreate && (
            <Button onClick={() => setDialog({ open: true })}>
              <Plus />
              {_t("Create label")}</Button>
          )
        }
      />
      <ErrorAlert error={labels.error} title={_t("Labels could not be loaded")} />
      <DataTable
        columns={columns}
        rows={labels.data?.items ?? []}
        rowKey={(l) => l.id}
        isLoading={labels.isLoading}
        emptyMessage={_t("No labels yet.")}
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
