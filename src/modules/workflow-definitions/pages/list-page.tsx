"use client";

import { useCallback, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChevronDown, Copy, Download, Eye, FileUp, MoreHorizontal, Pencil, Play, Plus, Rocket, Trash2, Undo2 } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { DataTable, type DataTableColumn, type SortState } from "@/components/common/data-table";
import { useConfirm } from "@/components/common/confirm-dialog";
import { describeError } from "@/components/common/error-alert";
import { PageHeader } from "@/components/common/page-header";
import { SearchInput } from "@/components/common/search-input";
import { FilterChips } from "@/components/common/filter-chips";
import { Timestamp } from "@/components/common/timestamp";
import { useStudioConfig } from "@/config/provider";
import type { OrderDirection } from "@/lib/api/types/common";
import type { WorkflowDefinitionSummary } from "@/lib/api/types/workflow-definitions";
import { useExtensions } from "@/lib/modules/registry";
import { href } from "@/lib/modules/router";
import { Resources, usePermissions, Verbs } from "@/lib/permissions";
import { downloadBlob, pickFiles } from "@/lib/utils/download";
import { useUrlState } from "@/lib/utils/url-state";
import { useElsaClient } from "@/lib/api/provider";
import { CreateWorkflowDialog } from "../components/create-workflow-dialog";
import { RunWorkflowDialog } from "../components/run-workflow-dialog";
import { useWorkflowDefinitionMutations, useWorkflowDefinitions } from "../hooks";
import { WorkflowDefinitionRoutes } from "../routes";

const SORT_KEYS: Record<string, "Name" | "Created" | "Version"> = { Name: "Name", Created: "Created", Version: "Version" };

export function WorkflowDefinitionsListPage() {
  const config = useStudioConfig();
  const url = useUrlState();
  const router = useRouter();
  const confirm = useConfirm();
  const permissions = usePermissions();
  const client = useElsaClient();
  const rowExtensions = useExtensions("definitionRowActions");
  const mutations = useWorkflowDefinitionMutations();

  const page = url.getNumber("page", 0);
  const pageSize = url.getNumber("pageSize", config.ui.defaultPageSize);
  const search = url.get("search") ?? "";
  const scope = (url.get("scope") as "all" | "published" | "drafts" | null) ?? "all";
  const sort: SortState = { key: url.get("orderBy") ?? "Name", direction: (url.get("orderDirection") as OrderDirection) ?? "Ascending" };

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [createOpen, setCreateOpen] = useState(() => url.get("create") === "1");
  const [runTarget, setRunTarget] = useState<WorkflowDefinitionSummary | null>(null);

  const latest = useWorkflowDefinitions({
    page,
    pageSize,
    searchTerm: search || undefined,
    versionOptions: scope === "published" ? "Published" : scope === "drafts" ? "Draft" : "Latest",
    isSystem: false,
    orderBy: SORT_KEYS[sort.key] ?? "Name",
    orderDirection: sort.direction,
  });

  // Rows whose latest version is not the published one need a second lookup to show the published version number.
  const unpublishedIds = useMemo(() => (latest.data?.items ?? []).filter((d) => !d.isPublished).map((d) => d.definitionId), [latest.data]);
  const published = useWorkflowDefinitions({ definitionIds: unpublishedIds, versionOptions: "Published", pageSize: unpublishedIds.length || 1 }, { enabled: unpublishedIds.length > 0 });
  const publishedVersionById = useMemo(() => new Map((published.data?.items ?? []).map((d) => [d.definitionId, d.version])), [published.data]);

  const canWrite = permissions.has(Resources.workflowDefinitions, Verbs.write) || permissions.has(Resources.workflowDefinitions, Verbs.update);
  const canCreate = permissions.has(Resources.workflowDefinitions, Verbs.create);
  const canDelete = permissions.has(Resources.workflowDefinitions, Verbs.delete);
  const canPublish = permissions.has(Resources.workflowDefinitions, Verbs.publish);
  const canRetract = permissions.has(Resources.workflowDefinitions, Verbs.retract);
  const canExecute = permissions.has(Resources.workflowDefinitions, Verbs.execute);
  // Read-only deployments omit the publish links from the response.
  const listReadOnly = !!latest.data && !latest.data.links?.some((l) => l.rel === "bulk-publish") && (latest.data.links?.length ?? 0) > 0;

  const run = useCallback(
    async (label: string, action: () => Promise<unknown>) => {
      try {
        await action();
        toast.success(label);
      } catch (e) {
        toast.error(describeError(e));
      }
    },
    [],
  );

  const exportDefinition = async (d: WorkflowDefinitionSummary) => {
    try {
      const blob = await client.workflowDefinitions.export(d.definitionId, "Latest");
      downloadBlob(blob, `${(d.name ?? d.definitionId).replace(/[^\w.-]+/g, "_")}_v${d.version}.json`);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const importFiles = async () => {
    const files = await pickFiles(".json,.zip", true);
    if (!files.length) return;
    await run(`Imported ${files.length} file(s)`, () => mutations.importFiles.mutateAsync(files));
  };

  const duplicate = async (d: WorkflowDefinitionSummary) => {
    try {
      const full = await client.workflowDefinitions.getByDefinitionId(d.definitionId, "Latest");
      const { toModel } = await import("../hooks");
      const model = toModel(full);
      const result = await mutations.save.mutateAsync({
        model: { ...model, definitionId: undefined, id: undefined, version: undefined, name: `${full.name ?? "Workflow"} (copy)` },
        publish: false,
      });
      toast.success("Workflow duplicated");
      router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: result.workflowDefinition.definitionId }));
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const deleteOne = async (d: WorkflowDefinitionSummary) => {
    if (!(await confirm({ title: `Delete "${d.name ?? d.definitionId}"?`, description: "All versions and their instances' references will be removed. This cannot be undone.", destructive: true, confirmLabel: "Delete" }))) return;
    await run("Workflow deleted", () => mutations.remove.mutateAsync(d.definitionId));
  };

  const bulk = async (kind: "delete" | "publish" | "retract" | "export") => {
    const ids = [...selected];
    if (ids.length === 0) return;
    if (kind === "delete") {
      if (!(await confirm({ title: `Delete ${ids.length} workflow(s)?`, description: "This cannot be undone.", destructive: true, confirmLabel: "Delete" }))) return;
      await run(`Deleted ${ids.length} workflow(s)`, () => mutations.bulkDelete.mutateAsync(ids));
    } else if (kind === "publish") {
      await run(`Published ${ids.length} workflow(s)`, () => mutations.bulkPublish.mutateAsync(ids));
    } else if (kind === "retract") {
      await run(`Unpublished ${ids.length} workflow(s)`, () => mutations.bulkRetract.mutateAsync(ids));
    } else {
      const rows = (latest.data?.items ?? []).filter((d) => selected.has(d.definitionId));
      try {
        const blob = await client.workflowDefinitions.bulkExport(rows.map((r) => r.id));
        downloadBlob(blob, `workflow-definitions-${new Date().toISOString().slice(0, 10)}.zip`);
      } catch (e) {
        toast.error(describeError(e));
      }
    }
    setSelected(new Set());
  };

  const helpers = { refresh: () => void latest.refetch(), navigate: (h: string) => router.push(h) };

  const columns: DataTableColumn<WorkflowDefinitionSummary>[] = [
    {
      id: "name",
      header: "Name",
      sortKey: "Name",
      cell: (d) => (
        <div className="min-w-0">
          <Link href={href(WorkflowDefinitionRoutes.edit, { definitionId: d.definitionId })} className="font-medium hover:underline">
            {d.name ?? <span className="text-muted-foreground italic">(unnamed)</span>}
          </Link>
          <div className="truncate text-xs text-muted-foreground font-mono">{d.definitionId}</div>
        </div>
      ),
    },
    {
      id: "description",
      header: "Description",
      hideable: true,
      className: "max-w-md",
      cell: (d) =>
        d.description ? (
          <Tooltip>
            <TooltipTrigger render={<span className="block truncate text-muted-foreground" />}>{d.description}</TooltipTrigger>
            <TooltipContent className="max-w-sm whitespace-pre-wrap">{d.description}</TooltipContent>
          </Tooltip>
        ) : (
          <span className="text-muted-foreground">–</span>
        ),
    },
    { id: "latest", header: "Latest", sortKey: "Version", hideable: true, cell: (d) => <Badge variant="outline">v{d.version}</Badge> },
    {
      id: "published",
      header: "Published",
      cell: (d) => {
        const v = d.isPublished ? d.version : publishedVersionById.get(d.definitionId);
        return v ? <Badge className="bg-emerald-500/15 text-emerald-700 dark:text-emerald-300 border-transparent">v{v}</Badge> : <span className="text-muted-foreground">–</span>;
      },
    },
    { id: "created", header: "Created", sortKey: "Created", hideable: true, cell: (d) => <Timestamp value={d.createdAt} relative /> },
    {
      id: "actions",
      header: "",
      width: "3rem",
      cell: (d) => {
        const rowReadOnly = listReadOnly || d.isReadonly || !canWrite;
        const publishedVersion = d.isPublished ? d.version : publishedVersionById.get(d.definitionId);
        return (
          <DropdownMenu>
            <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label="Actions" onClick={(e) => e.stopPropagation()} />}>
              <MoreHorizontal />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" onClick={(e) => e.stopPropagation()}>
              <DropdownMenuItem onClick={() => router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: d.definitionId }))}>
                {rowReadOnly ? <Eye /> : <Pencil />} {rowReadOnly ? "View" : "Edit"}
              </DropdownMenuItem>
              {canExecute && publishedVersion && (
                <DropdownMenuItem onClick={() => setRunTarget(d)}>
                  <Play /> Run
                </DropdownMenuItem>
              )}
              {canCreate && !listReadOnly && (
                <DropdownMenuItem onClick={() => void duplicate(d)}>
                  <Copy /> Duplicate
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={() => void exportDefinition(d)}>
                <Download /> Export
              </DropdownMenuItem>
              {!listReadOnly && (canPublish || canRetract) && <DropdownMenuSeparator />}
              {!listReadOnly && canPublish && !d.isPublished && (
                <DropdownMenuItem onClick={() => void run("Workflow published", () => mutations.publish.mutateAsync(d.definitionId))}>
                  <Rocket /> Publish
                </DropdownMenuItem>
              )}
              {!listReadOnly && canRetract && publishedVersion && (
                <DropdownMenuItem onClick={() => void run("Workflow unpublished", () => mutations.retract.mutateAsync(d.definitionId))}>
                  <Undo2 /> Unpublish
                </DropdownMenuItem>
              )}
              {rowExtensions
                .filter((a) => (!a.permissions || permissions.hasAny(a.permissions)) && (a.isVisible?.(d) ?? true))
                .map((a) => (
                  <DropdownMenuItem key={a.id} onClick={() => void a.onSelect(d, helpers)}>
                    {a.icon && <a.icon />} {a.label}
                  </DropdownMenuItem>
                ))}
              {canDelete && !listReadOnly && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem variant="destructive" onClick={() => void deleteOne(d)}>
                    <Trash2 /> Delete
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        );
      },
    },
  ];

  return (
    <div className="flex flex-col gap-3">
      <PageHeader
        title="Workflow definitions"
        description={latest.data ? `${latest.data.totalCount} definition(s)` : undefined}
        actions={
          <>
            {selected.size > 0 && (
              <DropdownMenu>
                <DropdownMenuTrigger render={<Button variant="outline" />}>
                  Bulk actions ({selected.size}) <ChevronDown />
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {canPublish && !listReadOnly && <DropdownMenuItem onClick={() => void bulk("publish")}><Rocket /> Publish</DropdownMenuItem>}
                  {canRetract && !listReadOnly && <DropdownMenuItem onClick={() => void bulk("retract")}><Undo2 /> Unpublish</DropdownMenuItem>}
                  <DropdownMenuItem onClick={() => void bulk("export")}><Download /> Export</DropdownMenuItem>
                  {canDelete && !listReadOnly && (
                    <>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem variant="destructive" onClick={() => void bulk("delete")}><Trash2 /> Delete</DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            )}
            {canCreate && !listReadOnly && (
              <div className="flex">
                <Button className="rounded-r-none" onClick={() => setCreateOpen(true)}>
                  <Plus /> Create workflow
                </Button>
                <DropdownMenu>
                  <DropdownMenuTrigger render={<Button className="rounded-l-none border-l border-primary-foreground/20 px-1.5" aria-label="More" />}>
                    <ChevronDown />
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => void importFiles()}>
                      <FileUp /> Import JSON / ZIP
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            )}
          </>
        }
      >
        {listReadOnly && (
          <div className="rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-800 dark:text-amber-200">
            The server runs in read-only mode: definitions can be viewed but not changed.
          </div>
        )}
      </PageHeader>

      <DataTable
        columnsStorageKey="workflow-definitions"
        toolbar={
          <>
            <SearchInput value={search} onChange={(v) => url.set({ search: v, page: 0 })} placeholder="Search by name, description or id…" className="w-full max-w-md" />
            <FilterChips
              value={scope}
              onChange={(v) => url.set({ scope: v === "all" ? undefined : v, page: 0 })}
              options={[
                { value: "all", label: "All" },
                { value: "published", label: "Published" },
                { value: "drafts", label: "Drafts" },
              ]}
            />
          </>
        }
        columns={columns}
        rows={latest.data?.items ?? []}
        rowKey={(d) => d.definitionId}
        isLoading={latest.isFetching}
        emptyMessage={search ? "No workflows match your search." : "No workflow definitions yet. Create one to get started."}
        selection={{ selected, onChange: setSelected }}
        sort={sort}
        onSortChange={(s) => url.set({ orderBy: s.key, orderDirection: s.direction })}
        paging={{ page, pageSize, totalCount: latest.data?.totalCount ?? 0, onChange: (p, ps) => url.set({ page: p, pageSize: ps }) }}
        onRowClick={(d) => router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: d.definitionId }))}
      />

      <CreateWorkflowDialog open={createOpen} onOpenChange={setCreateOpen} />
      <RunWorkflowDialog definition={runTarget} onOpenChange={(o) => !o && setRunTarget(null)} />
    </div>
  );
}
