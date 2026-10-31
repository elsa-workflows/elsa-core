"use client";
import { message, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ChevronLeft, ChevronRight, RefreshCw } from "lucide-react";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { useCallback, useState } from "react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Switch } from "@elsa-workflows/react-studio-ui/switch";
import { DataTable, type DataTableColumn } from "@elsa-workflows/react-studio-ui/common/data-table";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { SearchInput } from "@elsa-workflows/react-studio-ui/common/search-input";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { useStudioConfig } from "@elsa-workflows/react-studio-sdk/config-provider";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { useUrlState } from "@elsa-workflows/react-studio-sdk/utils/url-state";
import { type ListUserTasksArgs, type UserTaskDue, type UserTaskScope, type UserTaskStatus, type UserTaskSummary, UserTaskStatuses, participantLabel } from "../api";
import { PriorityBadge, UserTaskStatusBadge } from "../components/user-task-status-badge";
import { UserTasksGate } from "../components/user-tasks-gate";
import { useUserTaskCapabilities, useUserTasks } from "../hooks";
import { UserTaskRoutes } from "../routes";

const scopeOptions: { value: UserTaskScope; label: string }[] = [
  { value: "assigned", label: message("Assigned to me") },
  { value: "available", label: message("Available to me") },
  { value: "history", label: message("My history") },
  { value: "all", label: message("All tasks") },
  { value: "needs-attention", label: message("Needs attention") },
];

const dueOptions: { value: Exclude<UserTaskDue, "">; label: string }[] = [
  { value: "overdue", label: message("Overdue") },
  { value: "today", label: message("Due today") },
  { value: "week", label: message("Due this week") },
  { value: "none", label: message("No due date") },
];

const sortOptions = [
  { value: "created", label: message("Created") },
  { value: "updated", label: message("Updated") },
  { value: "due", label: message("Due") },
  { value: "priority", label: message("Priority") },
];

export function UserTasksListPage() {
  useTranslation();
  return (
    <UserTasksGate>
      <UserTasksList />
    </UserTasksGate>
  );
}

function UserTasksList() {
  const _t = useTranslation();
  const router = useRouter();
  const url = useUrlState();
  const { ui } = useStudioConfig();
  const { capabilities } = useUserTaskCapabilities();

  const scope = (url.get("scope") as UserTaskScope | undefined) ?? (capabilities?.canReadAll ? "all" : "assigned");
  const status = url.get("status") as UserTaskStatus | undefined;
  const due = url.get("due") as Exclude<UserTaskDue, ""> | undefined;
  const search = url.get("q") ?? "";
  const priorityFrom = url.get("pmin");
  const priorityTo = url.get("pmax");
  const sort = url.get("sort") ?? "created";
  const direction = (url.get("dir") as "asc" | "desc" | undefined) ?? "desc";
  const limit = url.getNumber("limit", ui.defaultPageSize);
  const cursor = url.get("cursor");
  const [cursorStack, setCursorStack] = useState<string[]>([]);
  const [poll, setPoll] = useState(false);

  const args: ListUserTasksArgs = {
    scope,
    cursor,
    limit,
    sort,
    direction,
    status: status ? [status] : undefined,
    due,
    search: search || undefined,
    priorityFrom: priorityFrom ? Number(priorityFrom) : undefined,
    priorityTo: priorityTo ? Number(priorityTo) : undefined,
    includeTotalCount: true,
  };
  const query = useUserTasks(args, { poll });

  const resetPaging = useCallback(
    (updates: Record<string, string | number | undefined>) => {
      setCursorStack([]);
      url.set({ ...updates, cursor: undefined });
    },
    [url],
  );

  const onSearch = useCallback((q: string) => resetPaging({ q }), [resetPaging]);

  const next = () => {
    const nextCursor = query.data?.nextCursor;
    if (!nextCursor) return;
    setCursorStack((s) => [...s, cursor ?? ""]);
    url.set({ cursor: nextCursor });
  };
  const prev = () => {
    const stack = [...cursorStack];
    const previous = stack.pop();
    setCursorStack(stack);
    url.set({ cursor: previous || undefined });
  };

  const columns: DataTableColumn<UserTaskSummary>[] = [
    {
      id: "title",
      header: _t("Task"),
      cell: (t) => (
        <div className="min-w-0">
          <div className="font-medium truncate">{t.title}</div>
          <div className="text-xs text-muted-foreground truncate">
            {t.reference && <span className="font-mono mr-2">{t.reference}</span>}
            {t.summary}
          </div>
        </div>
      ),
    },
    {
      id: "status",
      header: _t("Status"),
      cell: (t) => (
        <div className="flex flex-wrap gap-1">
          <UserTaskStatusBadge status={t.status} />
          {t.isOverdue && <ToneBadge tone="danger">{_t("Overdue")}</ToneBadge>}
          {t.healthSeverity && <ToneBadge tone={t.healthSeverity === "Blocking" ? "danger" : "warning"}>{t.healthCode ?? t.healthSeverity}</ToneBadge>}
        </div>
      ),
    },
    { id: "priority", header: _t("Priority"), cell: (t) => <PriorityBadge priority={t.priority} /> },
    {
      id: "assignee",
      header: _t("Assignee"),
      cell: (t) => (t.assignee ? participantLabel(t.assignee) : <span className="text-muted-foreground text-xs">{t.candidateSummary ?? "–"}</span>),
    },
    {
      id: "workflow",
      header: _t("Workflow"),
      cell: (t) => (
        <div className="text-xs">
          <div className="truncate">{t.workflowDefinitionName ?? t.workflowDefinitionId ?? "–"}</div>
          {t.workflowInstanceReference && <div className="font-mono text-muted-foreground truncate">{t.workflowInstanceReference}</div>}
        </div>
      ),
    },
    { id: "due", header: _t("Due"), cell: (t) => <Timestamp value={t.dueAt} relative /> },
    { id: "created", header: _t("Created"), cell: (t) => <Timestamp value={t.createdAt} relative /> },
  ];

  const items = query.data?.items ?? [];
  const total = query.data?.totalCount;

  return (
    <div className="flex flex-col gap-3">
      <PageHeader
        title={_t("User tasks")}
        description={_t("Human tasks raised by running workflows.")}
        actions={
          <>
            <Field inline label={_t("Auto-refresh")} htmlFor="ut-poll">
              <Switch id="ut-poll" checked={poll} onCheckedChange={setPoll} />
            </Field>
            <Button variant="outline" size="sm" onClick={() => query.refetch()} disabled={query.isFetching}>
              <RefreshCw data-icon="inline-start" className={query.isFetching ? "animate-spin" : undefined} />
              {_t("Refresh")}</Button>
          </>
        }
      >
        <div className="flex flex-wrap items-end gap-2">
          <SearchInput value={search} onChange={onSearch} placeholder={_t("Search title, reference, summary…")} className="w-72" />
          <SimpleSelect
            className="w-44"
            value={scope}
            options={scopeOptions.filter((o) => capabilities?.canReadAll || (o.value !== "all" && o.value !== "needs-attention")).map(option => ({ ...option, label: _t(option.label) }))}
            onChange={(v) => resetPaging({ scope: v })}
          />
          <SimpleSelect
            className="w-40"
            placeholder={_t("Any status")}
            allowEmpty
            value={status}
            options={UserTaskStatuses.map((s) => ({ value: s, label: _t(s) }))}
            onChange={(v) => resetPaging({ status: v })}
          />
          <SimpleSelect className="w-40" placeholder={_t("Any due date")} allowEmpty value={due} options={dueOptions.map(option => ({ ...option, label: _t(option.label) }))} onChange={(v) => resetPaging({ due: v })} />
          <div className="flex items-center gap-1">
            <Input type="number" min={0} max={100} placeholder={_t("P min")} className="w-20" value={priorityFrom ?? ""} onChange={(e) => resetPaging({ pmin: e.target.value })} />
            <span className="text-muted-foreground text-xs">–</span>
            <Input type="number" min={0} max={100} placeholder={_t("P max")} className="w-20" value={priorityTo ?? ""} onChange={(e) => resetPaging({ pmax: e.target.value })} />
          </div>
          <SimpleSelect className="w-32" value={sort} options={sortOptions.map(option => ({ ...option, label: _t(option.label) }))} onChange={(v) => resetPaging({ sort: v })} />
          <SimpleSelect
            className="w-28"
            value={direction}
            options={[
              { value: "desc", label: _t("Newest") },
              { value: "asc", label: _t("Oldest") },
            ]}
            onChange={(v) => resetPaging({ dir: v })}
          />
        </div>
      </PageHeader>

      <ErrorAlert error={query.error} title={_t("Could not load user tasks")} />
      <DataTable columns={columns} rows={items} rowKey={(t) => t.id} isLoading={query.isLoading} onRowClick={(t) => router.push(href(UserTaskRoutes.detail, { taskId: t.id }))} emptyMessage={_t("No tasks match the current filters.")} />

      <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
        <div className="flex items-center gap-2">
          <span>{_t("Per page")}</span>
          <SimpleSelect
            size="sm"
            className="w-20"
            value={String(limit)}
            options={["10", "25", "50", "100"].map((s) => ({ value: s, label: _t(s) }))}
            onChange={(v) => v && resetPaging({ limit: v })}
          />
        </div>
        <div className="flex items-center gap-2">
          <span>
            {items.length} {_t("shown")}{typeof total === "number" ? _t(" of {{value0}}", { value0: total }) : ""}
          </span>
          <Button variant="outline" size="icon-sm" onClick={prev} disabled={cursorStack.length === 0 && !cursor} aria-label={_t("Previous page")}>
            <ChevronLeft />
          </Button>
          <Button variant="outline" size="icon-sm" onClick={next} disabled={!query.data?.nextCursor} aria-label={_t("Next page")}>
            <ChevronRight />
          </Button>
        </div>
      </div>
    </div>
  );
}
