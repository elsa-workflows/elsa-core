"use client";

import { ExternalLink, RefreshCw, Trash2, Wrench } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { DataTable, type DataTableColumn } from "@/components/common/data-table";
import { EmptyState } from "@/components/common/empty-state";
import { PageHeader } from "@/components/common/page-header";
import { Timestamp } from "@/components/common/timestamp";
import { ToneBadge } from "@/components/common/status-badge";
import { useConfirm } from "@/components/common/confirm-dialog";
import { describeError } from "@/components/common/error-alert";
import { href } from "@/lib/modules/router";
import { usePermission, Resources, Verbs } from "@/lib/permissions";
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
      header: "Plan",
      cell: (r) => <span className="font-mono text-xs">{r.ref.id}</span>,
    },
    {
      id: "status",
      header: "Status",
      cell: (r) =>
        r.error ? (
          <ToneBadge tone="danger" className="max-w-56 truncate" >
            {describeError(r.error)}
          </ToneBadge>
        ) : r.isLoading ? (
          <span className="text-muted-foreground text-xs">Loading…</span>
        ) : (
          <PlanStatusBadge status={r.data?.plan.status} />
        ),
    },
    {
      id: "jobs",
      header: "Jobs",
      cell: (r) => {
        if (!r.data) return <span className="text-muted-foreground">–</span>;
        const jobs = r.data.jobs;
        const done = jobs.filter((j) => j.status === "Completed").length;
        const failed = jobs.filter((j) => j.status === "Failed").length;
        return (
          <span className="tabular-nums text-xs">
            {done}/{jobs.length} completed{failed ? `, ${failed} failed` : ""}
          </span>
        );
      },
    },
    { id: "alterations", header: "Alterations", cell: (r) => <span className="tabular-nums">{r.data?.plan.alterations.length ?? r.ref.alterationCount}</span> },
    { id: "submitted", header: "Submitted", cell: (r) => <Timestamp value={r.data?.plan.createdAt ?? r.ref.submittedAt} relative /> },
    { id: "completed", header: "Completed", cell: (r) => <Timestamp value={r.data?.plan.completedAt} relative /> },
    {
      id: "actions",
      header: "",
      className: "text-right",
      cell: (r) => (
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label="Forget plan"
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
        title="Alteration plans"
        description="Plans submitted from this browser. The server does not list plans, so only plans you submitted or opened here are shown."
        actions={
          <>
            {canExecute && (
              <Button variant="outline" onClick={() => openRetry([])}>
                <RefreshCw data-icon="inline-start" />
                Retry faulted instances
              </Button>
            )}
            {rows.length > 0 && (
              <Button
                variant="ghost"
                onClick={async () => {
                  if (await confirm({ title: "Clear recent plans?", description: "Only this browser's list is cleared; the plans themselves are untouched.", destructive: true, confirmLabel: "Clear" }))
                    clear();
                }}
              >
                Clear list
              </Button>
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
          <Input value={lookup} onChange={(e) => setLookup(e.target.value)} placeholder="Open a plan by id…" className="font-mono text-xs" />
          <Button type="submit" variant="outline" disabled={!lookup.trim()}>
            <ExternalLink data-icon="inline-start" />
            Open
          </Button>
        </form>
      </PageHeader>

      {rows.length === 0 ? (
        <EmptyState
          icon={Wrench}
          title="No alteration plans yet"
          description="Use “Alter” on a running workflow instance to compose alterations and submit them as a plan, or run them immediately."
        />
      ) : (
        <DataTable columns={columns} rows={data} rowKey={(r) => r.ref.id} onRowClick={(r) => router.push(href(AlterationRoutes.plan, { id: r.ref.id }))} />
      )}
    </div>
  );
}
