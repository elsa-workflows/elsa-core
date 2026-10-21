"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ExternalLink, RefreshCw, Trash2, Wrench } from "lucide-react";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { useState } from "react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { DataTable, type DataTableColumn } from "@elsa-workflows/react-studio-ui/common/data-table";
import { EmptyState } from "@elsa-workflows/react-studio-ui/common/empty-state";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { usePermission, Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { useRecentPlanStatuses } from "../hooks";
import { useAlterationDialogs } from "../dialog-store";
import { PlanStatusBadge } from "../components/plan-status-badge";
import { AlterationRoutes } from "../routes";
import type { RecentPlanRef } from "../recent-plans";
import type { GetAlterationPlanResponse } from "../api";

interface Row {
  ref: RecentPlanRef;
  data?: GetAlterationPlanResponse;
  error?: unknown;
  isLoading: boolean;
}

/**
 * `Elsa.Alterations` has no "list plans" endpoint, so this page lists the plans submitted from this
 * browser (remembered in localStorage) and fetches each one's status.
 */
export function AlterationPlansPage() {
  const _t = useTranslation();
  const router = useRouter();
  const confirm = useConfirm();
  const canExecute = usePermission(Resources.alterations, Verbs.execute);
  const { rows, forget, clear, remember } = useRecentPlanStatuses();
  const { openRetry } = useAlterationDialogs();
  const [lookup, setLookup] = useState("");

  const data: Row[] = rows.map(({ ref, query }) => ({ ref, data: query.data, error: query.error ?? undefined, isLoading: query.isLoading }));

  const columns: DataTableColumn<Row>[] = [
    {
      id: "id",
      header: _t("Plan"),
      cell: (r) => <span className="font-mono text-xs">{r.ref.id}</span>,
    },
    {
      id: "status",
      header: _t("Status"),
      cell: (r) =>
        r.error ? (
          <ToneBadge tone="danger" className="max-w-56 truncate" >
            {describeError(r.error)}
          </ToneBadge>
        ) : r.isLoading ? (
          <span className="text-muted-foreground text-xs">{_t("Loading…")}</span>
        ) : (
          <PlanStatusBadge status={r.data?.plan.status} />
        ),
    },
    {
      id: "jobs",
      header: _t("Jobs"),
      cell: (r) => {
        if (!r.data) return <span className="text-muted-foreground">–</span>;
        const jobs = r.data.jobs;
        const done = jobs.filter((j) => j.status === "Completed").length;
        const failed = jobs.filter((j) => j.status === "Failed").length;
        return (
          <span className="tabular-nums text-xs">
            {done}/{jobs.length} {_t("completed")}{failed ? _t(", {{value0}} failed", { value0: failed }) : ""}
          </span>
        );
      },
    },
    { id: "alterations", header: _t("Alterations"), cell: (r) => <span className="tabular-nums">{r.data?.plan.alterations.length ?? r.ref.alterationCount}</span> },
    { id: "submitted", header: _t("Submitted"), cell: (r) => <Timestamp value={r.data?.plan.createdAt ?? r.ref.submittedAt} relative /> },
    { id: "completed", header: _t("Completed"), cell: (r) => <Timestamp value={r.data?.plan.completedAt} relative /> },
    {
      id: "actions",
      header: "",
      className: "text-right",
      cell: (r) => (
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={_t("Forget plan")}
          onClick={(e) => {
            e.stopPropagation();
            forget(r.ref.id);
          }}
        >
          <Trash2 />
        </Button>
      ),
    },
  ];

  const openById = () => {
    const id = lookup.trim();
    if (!id) return;
    remember({ id, submittedAt: new Date().toISOString(), alterationCount: 0 });
    setLookup("");
    router.push(href(AlterationRoutes.plan, { id }));
  };

  return (
    <div>
      <PageHeader
        title={_t("Alteration plans")}
        description={_t("Plans submitted from this browser. The server does not list plans, so only plans you submitted or opened here are shown.")}
        actions={
          <>
            {canExecute && (
              <Button variant="outline" onClick={() => openRetry([])}>
                <RefreshCw data-icon="inline-start" />
                {_t("Retry faulted instances")}</Button>
            )}
            {rows.length > 0 && (
              <Button
                variant="ghost"
                onClick={async () => {
                  if (await confirm({ title: _t("Clear recent plans?"), description: _t("Only this browser's list is cleared; the plans themselves are untouched."), destructive: true, confirmLabel: _t("Clear") }))
                    clear();
                }}
              >
                {_t("Clear list")}</Button>
            )}
          </>
        }
      >
        <form
          className="flex items-center gap-2 max-w-lg"
          onSubmit={(e) => {
            e.preventDefault();
            openById();
          }}
        >
          <Input value={lookup} onChange={(e) => setLookup(e.target.value)} placeholder={_t("Open a plan by id…")} className="font-mono text-xs" />
          <Button type="submit" variant="outline" disabled={!lookup.trim()}>
            <ExternalLink data-icon="inline-start" />
            {_t("Open")}</Button>
        </form>
      </PageHeader>

      {rows.length === 0 ? (
        <EmptyState
          icon={Wrench}
          title={_t("No alteration plans yet")}
          description={_t("Use “Alter” on a running workflow instance to compose alterations and submit them as a plan, or run them immediately.")}
        />
      ) : (
        <DataTable columns={columns} rows={data} rowKey={(r) => r.ref.id} onRowClick={(r) => router.push(href(AlterationRoutes.plan, { id: r.ref.id }))} />
      )}
    </div>
  );
}
