"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useCallback, useMemo, useState } from "react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { ChevronDown, Copy, Download, Eye, FileUp, MoreHorizontal, Pencil, Play, Plus, Rocket, Trash2, Undo2 } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@elsa-workflows/react-studio-ui/badge";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@elsa-workflows/react-studio-ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@elsa-workflows/react-studio-ui/tooltip";
import { DataTable, type DataTableColumn, type SortState } from "@elsa-workflows/react-studio-ui/common/data-table";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { SearchInput } from "@elsa-workflows/react-studio-ui/common/search-input";
import { FilterChips } from "@elsa-workflows/react-studio-ui/common/filter-chips";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { useStudioConfig } from "@elsa-workflows/react-studio-sdk/config-provider";
import type { OrderDirection } from "@elsa-workflows/react-studio-api-client/types/common";
import type { WorkflowDefinitionSummary } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import { useExtensions } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { Resources, usePermissions, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { downloadBlob, pickFiles } from "@elsa-workflows/react-studio-sdk/utils/download";
import { useUrlState } from "@elsa-workflows/react-studio-sdk/utils/url-state";
import { useElsaClient } from "@elsa-workflows/react-studio-api-client/react";
import { BpmnExportDialog, BpmnImportDialog } from "../components/bpmn-dialogs";
import { CreateWorkflowDialog } from "../components/create-workflow-dialog";
import { RunWorkflowDialog } from "../components/run-workflow-dialog";
import { useWorkflowDefinitionMutations, useWorkflowDefinitions } from "../hooks";
import { WorkflowDefinitionRoutes } from "../routes";

const SORT_KEYS: Record<string, "Name" | "Created" | "Version"> = { Name: "Name", Created: "Created", Version: "Version" };

export function WorkflowDefinitionsListPage() {
  const _t = useTranslation();
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
  const [bpmnImportOpen, setBpmnImportOpen] = useState(false);
  const [bpmnExportId, setBpmnExportId] = useState<string>();
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
      toast.success(_t("Workflow duplicated"));
      router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: result.workflowDefinition.definitionId }));
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const deleteOne = async (d: WorkflowDefinitionSummary) => {
    if (!(await confirm({ title: _t("Delete \"{{value0}}\"?", { value0: d.name ?? d.definitionId }), description: _t("All versions and their instances' references will be removed. This cannot be undone."), destructive: true, confirmLabel: _t("Delete") }))) return;
    await run(translate("Workflow deleted"), () => mutations.remove.mutateAsync(d.definitionId));
  };

  const bulk = async (kind: "delete" | "publish" | "retract" | "export") => {
    const ids = [...selected];
    if (ids.length === 0) return;
    if (kind === "delete") {
      if (!(await confirm({ title: _t("Delete {{value0}} workflow(s)?", { value0: ids.length }), description: _t("This cannot be undone."), destructive: true, confirmLabel: _t("Delete") }))) return;
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
      header: _t("Name"),
      sortKey: "Name",
      cell: (d) => (
        <div className="min-w-0">
          <Link href={href(WorkflowDefinitionRoutes.edit, { definitionId: d.definitionId })} className="font-medium hover:underline">
            {d.name ?? <span className="text-muted-foreground italic">{_t("(unnamed)")}</span>}
          </Link>
          <div className="truncate text-xs text-muted-foreground font-mono">{d.definitionId}</div>
        </div>
      ),
    },
    {
      id: "description",
      header: _t("Description"),
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
    { id: "latest", header: _t("Latest"), sortKey: "Version", hideable: true, cell: (d) => <Badge variant="outline">{_t("v")}{d.version}</Badge> },
    {
      id: "published",
      header: _t("Published"),
      cell: (d) => {
        const v = d.isPublished ? d.version : publishedVersionById.get(d.definitionId);
        return v ? <Badge className="bg-emerald-500/15 text-emerald-700 dark:text-emerald-300 border-transparent">{_t("v")}{v}</Badge> : <span className="text-muted-foreground">–</span>;
      },
    },
    { id: "created", header: _t("Created"), sortKey: "Created", hideable: true, cell: (d) => <Timestamp value={d.createdAt} relative /> },
    {
      id: "actions",
      header: "",
      width: "3rem",
      cell: (d) => {
        const rowReadOnly = listReadOnly || d.isReadonly || !canWrite;
        const publishedVersion = d.isPublished ? d.version : publishedVersionById.get(d.definitionId);
        return (
          <DropdownMenu>
            <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label={_t("Actions")} onClick={(e) => e.stopPropagation()} />}>
              <MoreHorizontal />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" onClick={(e) => e.stopPropagation()}>
              <DropdownMenuItem onClick={() => router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: d.definitionId }))}>
                {rowReadOnly ? <Eye /> : <Pencil />} {rowReadOnly ? _t("View") : _t("Edit")}
              </DropdownMenuItem>
              {canExecute && publishedVersion && (
                <DropdownMenuItem onClick={() => setRunTarget(d)}>
                  <Play /> {_t("Run")}</DropdownMenuItem>
              )}
              {canCreate && !listReadOnly && (
                <DropdownMenuItem onClick={() => void duplicate(d)}>
                  <Copy /> {_t("Duplicate")}</DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={() => void exportDefinition(d)}>
                <Download /> {_t("Export")}</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setBpmnExportId(d.definitionId)}><Download /> {_t("Export BPMN")}</DropdownMenuItem>
              {!listReadOnly && (canPublish || canRetract) && <DropdownMenuSeparator />}
              {!listReadOnly && canPublish && !d.isPublished && (
                <DropdownMenuItem onClick={() => void run(translate("Workflow published"), () => mutations.publish.mutateAsync(d.definitionId))}>
                  <Rocket /> {_t("Publish")}</DropdownMenuItem>
              )}
              {!listReadOnly && canRetract && publishedVersion && (
                <DropdownMenuItem onClick={() => void run(translate("Workflow unpublished"), () => mutations.retract.mutateAsync(d.definitionId))}>
                  <Undo2 /> {_t("Unpublish")}</DropdownMenuItem>
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
                    <Trash2 /> {_t("Delete")}</DropdownMenuItem>
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
        title={_t("Workflow definitions")}
        description={latest.data ? _t("{{value0}} definition(s)", { value0: latest.data.totalCount }) : undefined}
        actions={
          <>
            {selected.size > 0 && (
              <DropdownMenu>
                <DropdownMenuTrigger render={<Button variant="outline" />}>
                  {_t("Bulk actions (")}{selected.size}) <ChevronDown />
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {canPublish && !listReadOnly && <DropdownMenuItem onClick={() => void bulk("publish")}><Rocket /> {_t("Publish")}</DropdownMenuItem>}
                  {canRetract && !listReadOnly && <DropdownMenuItem onClick={() => void bulk("retract")}><Undo2 /> {_t("Unpublish")}</DropdownMenuItem>}
                  <DropdownMenuItem onClick={() => void bulk("export")}><Download /> {_t("Export")}</DropdownMenuItem>
                  {canDelete && !listReadOnly && (
                    <>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem variant="destructive" onClick={() => void bulk("delete")}><Trash2 /> {_t("Delete")}</DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            )}
            {permissions.has(Resources.workflowDefinitions, Verbs.write) && !listReadOnly && <Button variant="outline" onClick={() => setBpmnImportOpen(true)}><FileUp /> {_t("Import BPMN")}</Button>}
            {canCreate && !listReadOnly && (
              <div className="flex">
                <Button className="rounded-r-none" onClick={() => setCreateOpen(true)}>
                  <Plus /> {_t("Create workflow")}</Button>
                <DropdownMenu>
                  <DropdownMenuTrigger render={<Button className="rounded-l-none border-l border-primary-foreground/20 px-1.5" aria-label={_t("More")} />}>
                    <ChevronDown />
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => void importFiles()}>
                      <FileUp /> {_t("Import JSON / ZIP")}</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            )}
          </>
        }
      >
        {listReadOnly && (
          <div className="rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-800 dark:text-amber-200">
            {_t("The server runs in read-only mode: definitions can be viewed but not changed.")}</div>
        )}
      </PageHeader>

      <DataTable
        columnsStorageKey="workflow-definitions"
        toolbar={
          <>
            <SearchInput value={search} onChange={(v) => url.set({ search: v, page: 0 })} placeholder={_t("Search by name, description or id…")} className="w-full max-w-md" />
            <FilterChips
              value={scope}
              onChange={(v) => url.set({ scope: v === "all" ? undefined : v, page: 0 })}
              options={[
                { value: "all", label: _t("All") },
                { value: "published", label: _t("Published") },
                { value: "drafts", label: _t("Drafts") },
              ]}
            />
          </>
        }
        columns={columns}
        rows={latest.data?.items ?? []}
        rowKey={(d) => d.definitionId}
        isLoading={latest.isFetching}
        emptyMessage={search ? _t("No workflows match your search.") : _t("No workflow definitions yet. Create one to get started.")}
        selection={{ selected, onChange: setSelected }}
        sort={sort}
        onSortChange={(s) => url.set({ orderBy: s.key, orderDirection: s.direction })}
        paging={{ page, pageSize, totalCount: latest.data?.totalCount ?? 0, onChange: (p, ps) => url.set({ page: p, pageSize: ps }) }}
        onRowClick={(d) => router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: d.definitionId }))}
      />

      {bpmnImportOpen && <BpmnImportDialog onClose={() => setBpmnImportOpen(false)} onImported={result => { setBpmnImportOpen(false); void latest.refetch(); router.push(href(WorkflowDefinitionRoutes.edit, { definitionId: result.definitionId })); }} />}
      {bpmnExportId && <BpmnExportDialog definitionId={bpmnExportId} onClose={() => setBpmnExportId(undefined)} />}
      <CreateWorkflowDialog open={createOpen} onOpenChange={setCreateOpen} />
      <RunWorkflowDialog definition={runTarget} onOpenChange={(o) => !o && setRunTarget(null)} />
    </div>
  );
}
