"use client";

import { Ban, RefreshCw, Trash2, X } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/common/confirm-dialog";
import { DataTable, type DataTableColumn, type SortState } from "@/components/common/data-table";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge, ToneBadge } from "@/components/common/status-badge";
import { Timestamp } from "@/components/common/timestamp";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { useStudioConfig } from "@/config/provider";
import type { OrderDirection } from "@/lib/api/types/common";
import type { ListWorkflowInstancesArgs, TimestampFilter, WorkflowInstanceSummary, WorkflowStatus, WorkflowSubStatus } from "@/lib/api/types/workflow-instances";
import { href } from "@/lib/modules/router";
import type { ListActionHelpers } from "@/lib/modules/types";
import { usePermission, Resources, Verbs } from "@/lib/permissions";
import { useUrlState } from "@/lib/utils/url-state";
import { InstanceFilters, type HasIncidents, type InstanceFilterValues } from "../components/instance-filters";
import { InstanceRowActions } from "../components/instance-row-actions";
import {
  useBulkCancelWorkflowInstances,
  useBulkDeleteWorkflowInstances,
  useCancelWorkflowInstance,
  useDeleteWorkflowInstance,
  useWorkflowDefinitionOptions,
  useWorkflowInstances,
} from "../hooks";
import { WORKFLOW_STATUSES, WORKFLOW_SUB_STATUSES } from "../model";
import { WorkflowInstanceRoutes } from "../routes";

type OrderBy = NonNullable<ListWorkflowInstancesArgs["orderBy"]>;
const ORDER_BY_VALUES: OrderBy[] = ["Created", "UpdatedAt", "Finished", "Name"];

/** Reads a repeated query parameter as a referentially stable array (optionally restricted to `allowed`). */
function useListFromUrl<T extends string>(getAll: (key: string) => string[], key: string, allowed?: readonly T[]): T[] {
  const serialized = JSON.stringify(getAll(key));
  return useMemo(() => {
    const values = JSON.parse(serialized) as string[];
    return values.filter((v): v is T => !allowed || (allowed as readonly string[]).includes(v));
  }, [serialized, allowed]);
}

export function WorkflowInstancesListPage() {
  const router = useRouter();
  const confirm = useConfirm();
  const config = useStudioConfig();
  const url = useUrlState();
  const canDelete = usePermission(Resources.workflowInstances, Verbs.delete);
  const canCancel = usePermission(Resources.workflowInstances, Verbs.cancel);

  // --- list state (URL-backed where shareable) ---
  const page = url.getNumber("page", 0);
  const pageSize = url.getNumber("pageSize", config.ui.defaultPageSize);
  const search = url.get("search") ?? "";
  const definitionIds = useListFromUrl<string>(url.getAll, "definitionIds");
  const statuses = useListFromUrl<WorkflowStatus>(url.getAll, "status", WORKFLOW_STATUSES);
  const subStatuses = useListFromUrl<WorkflowSubStatus>(url.getAll, "subStatus", WORKFLOW_SUB_STATUSES);
  const hasIncidentsRaw = url.get("hasIncidents");
  const hasIncidents: HasIncidents | undefined = hasIncidentsRaw === "true" || hasIncidentsRaw === "false" ? hasIncidentsRaw : undefined;
  const orderByRaw = url.get("orderBy");
  const orderBy: OrderBy = ORDER_BY_VALUES.includes(orderByRaw as OrderBy) ? (orderByRaw as OrderBy) : "Created";
  const orderDirection: OrderDirection = url.get("orderDirection") === "Ascending" ? "Ascending" : "Descending";
  const [timestampFilters, setTimestampFilters] = useState<TimestampFilter[]>([]);
  const [polling, setPolling] = useState(true);
  const [selected, setSelected] = useState<Set<string>>(() => new Set());

  const args = useMemo<ListWorkflowInstancesArgs>(
    () => ({
      page,
      pageSize,
      searchTerm: search || undefined,
      definitionIds: definitionIds.length ? definitionIds : undefined,
      statuses: statuses.length ? statuses : undefined,
      subStatuses: subStatuses.length ? subStatuses : undefined,
      hasIncidents: hasIncidents === undefined ? undefined : hasIncidents === "true",
      timestampFilters: timestampFilters.length ? timestampFilters : undefined,
      orderBy,
      orderDirection,
    }),
    [page, pageSize, search, definitionIds, statuses, subStatuses, hasIncidents, timestampFilters, orderBy, orderDirection],
  );

  const list = useWorkflowInstances(args, { poll: polling });
  const definitions = useWorkflowDefinitionOptions();
  const { mutateAsync: deleteOne } = useDeleteWorkflowInstance();
  const deleteMany = useBulkDeleteWorkflowInstances();
  const { mutateAsync: cancelOne } = useCancelWorkflowInstance();
  const cancelMany = useBulkCancelWorkflowInstances();

  const rows = useMemo(() => list.data?.items ?? [], [list.data]);
  const totalCount = list.data?.totalCount ?? 0;

  const filterValues: InstanceFilterValues = { search, definitionIds, statuses, subStatuses, hasIncidents, timestampFilters };
  const setUrl = url.set;
  const activeFilterCount =
    (filterValues.search ? 1 : 0) +
    (filterValues.definitionIds.length ? 1 : 0) +
    (filterValues.statuses.length ? 1 : 0) +
    (filterValues.subStatuses.length ? 1 : 0) +
    (filterValues.hasIncidents !== undefined ? 1 : 0) +
    (filterValues.timestampFilters.length ? 1 : 0);

  const onFilterChange = useCallback(
    (patch: Partial<InstanceFilterValues>) => {
      if (patch.timestampFilters !== undefined) setTimestampFilters(patch.timestampFilters);
      setUrl({
        page: 0,
        ...(patch.search !== undefined ? { search: patch.search } : {}),
        ...(patch.definitionIds !== undefined ? { definitionIds: patch.definitionIds } : {}),
        ...(patch.statuses !== undefined ? { status: patch.statuses } : {}),
        ...(patch.subStatuses !== undefined ? { subStatus: patch.subStatuses } : {}),
        ...("hasIncidents" in patch ? { hasIncidents: patch.hasIncidents } : {}),
      });
    },
    [setUrl],
  );

  const refetch = list.refetch;
  const helpers = useMemo<ListActionHelpers>(() => ({ refresh: () => void refetch(), navigate: (to) => router.push(to) }), [refetch, router]);

  // --- actions ---
  const handleCancel = useCallback(
    async (instance: WorkflowInstanceSummary) => {
      if (!(await confirm({ title: "Cancel workflow instance?", description: `Instance ${instance.id} will be cancelled.`, confirmLabel: "Cancel instance" }))) return;
      try {
        await cancelOne(instance.id);
        toast.success("Workflow instance cancelled.");
      } catch (e) {
        toast.error(describeError(e));
      }
    },
    [confirm, cancelOne],
  );

  const handleDelete = useCallback(
    async (instance: WorkflowInstanceSummary) => {
      if (!(await confirm({ title: "Delete workflow instance?", description: `Instance ${instance.id} will be permanently deleted.`, confirmLabel: "Delete", destructive: true }))) return;
      try {
        await deleteOne(instance.id);
        setSelected((s) => {
          const next = new Set(s);
          next.delete(instance.id);
          return next;
        });
        toast.success("Workflow instance deleted.");
      } catch (e) {
        toast.error(describeError(e));
      }
    },
    [confirm, deleteOne],
  );

  const handleBulkCancel = async () => {
    const ids = [...selected];
    if (!(await confirm({ title: `Cancel ${ids.length} workflow instance(s)?`, confirmLabel: "Cancel instances" }))) return;
    try {
      const result = await cancelMany.mutateAsync(ids);
      toast.success(`${result?.cancelled ?? ids.length} workflow instance(s) cancelled.`);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const handleBulkDelete = async () => {
    const ids = [...selected];
    if (!(await confirm({ title: `Delete ${ids.length} workflow instance(s)?`, description: "This cannot be undone.", confirmLabel: "Delete", destructive: true }))) return;
    try {
      const result = await deleteMany.mutateAsync(ids);
      setSelected(new Set());
      toast.success(`${result?.deleted ?? ids.length} workflow instance(s) deleted.`);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  // --- table ---
  const nameOf = definitions.nameOf;
  const columns = useMemo<DataTableColumn<WorkflowInstanceSummary>[]>(
    () => [
      {
        id: "id",
        header: "ID",
        cell: (r) => (
          <Link href={href(WorkflowInstanceRoutes.view, { id: r.id })} className="font-mono text-xs text-primary hover:underline" onClick={(e) => e.stopPropagation()}>
            {r.id}
          </Link>
        ),
      },
      {
        id: "correlationId",
        hideable: true,
        header: "Correlation ID",
        cell: (r) => (r.correlationId ? <span className="font-mono text-xs">{r.correlationId}</span> : <span className="text-muted-foreground">–</span>),
      },
      { id: "workflow", header: "Workflow", cell: (r) => <span className="truncate">{nameOf(r.definitionId)}</span> },
      { id: "version",
        hideable: true, header: "Version", cell: (r) => <span className="tabular-nums">{r.version}</span>, className: "text-right", headerClassName: "text-right" },
      { id: "name",
        hideable: true, header: "Name", sortKey: "Name", cell: (r) => r.name ?? <span className="text-muted-foreground">–</span> },
      { id: "status", header: "Status", cell: (r) => <StatusBadge status={r.subStatus} /> },
      {
        id: "incidents",
        header: "Incidents",
        cell: (r) => (r.incidentCount > 0 ? <ToneBadge tone="danger">{r.incidentCount}</ToneBadge> : <span className="text-muted-foreground tabular-nums">0</span>),
      },
      { id: "created", header: "Created", sortKey: "Created", cell: (r) => <Timestamp value={r.createdAt} /> },
      { id: "updated",
        hideable: true, header: "Updated", sortKey: "UpdatedAt", cell: (r) => <Timestamp value={r.updatedAt} /> },
      { id: "finished",
        hideable: true, header: "Finished", sortKey: "Finished", cell: (r) => <Timestamp value={r.finishedAt} /> },
      {
        id: "actions",
        header: "",
        width: "3rem",
        className: "text-right",
        cell: (r) => <InstanceRowActions instance={r} helpers={helpers} onCancel={handleCancel} onDelete={handleDelete} />,
      },
    ],
    [nameOf, helpers, handleCancel, handleDelete],
  );

  const sort: SortState = { key: orderBy, direction: orderDirection };
  const onSortChange = (next: SortState) => url.set({ page: 0, orderBy: next.key, orderDirection: next.direction });

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Workflow instances"
        description={totalCount > 0 ? `${totalCount} instance${totalCount === 1 ? "" : "s"}` : undefined}
        actions={
          <div className="flex items-center gap-1 rounded-lg border bg-card p-0.5">
            <Label htmlFor="instances-poll" className="flex items-center gap-2 px-2 text-xs font-normal text-muted-foreground">
              <Switch id="instances-poll" size="sm" checked={polling} onCheckedChange={(checked) => setPolling(checked)} />
              Auto refresh
            </Label>
            <span className="h-4 w-px bg-border" />
            <Button variant="ghost" size="sm" onClick={() => void list.refetch()} disabled={list.isFetching}>
              <RefreshCw className={list.isFetching ? "animate-spin" : undefined} />
              Refresh
            </Button>
          </div>
        }
      />

      {list.error && <ErrorAlert error={list.error} title="Could not load workflow instances" />}

      {selected.size > 0 && (
        <div className="flex flex-wrap items-center gap-2 rounded-lg border bg-muted/40 px-3 py-2 text-sm">
          <span className="font-medium">{selected.size} selected</span>
          {canCancel && (
            <Button variant="outline" size="sm" onClick={() => void handleBulkCancel()} disabled={cancelMany.isPending}>
              <Ban />
              Cancel
            </Button>
          )}
          {canDelete && (
            <Button variant="destructive" size="sm" onClick={() => void handleBulkDelete()} disabled={deleteMany.isPending}>
              <Trash2 />
              Delete
            </Button>
          )}
          <Button variant="ghost" size="sm" className="ml-auto" onClick={() => setSelected(new Set())}>
            <X />
            Clear selection
          </Button>
        </div>
      )}

      <DataTable
        columnsStorageKey="workflow-instances"
        toolbar={
          <div className="flex w-full flex-wrap items-center gap-2">
            <InstanceFilters value={filterValues} onChange={onFilterChange} definitionOptions={definitions.options} />
            {activeFilterCount > 0 && (
              <Button
                variant="ghost"
                size="sm"
                className="text-muted-foreground"
                onClick={() => onFilterChange({ search: "", definitionIds: [], statuses: [], subStatuses: [], hasIncidents: undefined, timestampFilters: [] })}
              >
                <X /> Clear {activeFilterCount} filter{activeFilterCount === 1 ? "" : "s"}
              </Button>
            )}
          </div>
        }
        columns={columns}
        rows={rows}
        rowKey={(r) => r.id}
        isLoading={list.isLoading}
        emptyMessage="No workflow instances match the current filters."
        selection={canDelete || canCancel ? { selected, onChange: setSelected } : undefined}
        sort={sort}
        onSortChange={onSortChange}
        paging={{ page, pageSize, totalCount, onChange: (p, size) => url.set({ page: p, pageSize: size }) }}
        onRowClick={(r) => router.push(href(WorkflowInstanceRoutes.view, { id: r.id }))}
      />
    </div>
  );
}
