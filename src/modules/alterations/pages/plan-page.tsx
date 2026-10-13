"use client";

import { ArrowLeft, RefreshCw } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/common/data-table";
import { ErrorAlert } from "@/components/common/error-alert";
import { JsonViewer } from "@/components/common/json-viewer";
import { KeyValueTable } from "@/components/common/key-value-table";
import { PageHeader } from "@/components/common/page-header";
import { Timestamp } from "@/components/common/timestamp";
import type { RouteParams } from "@/lib/modules/types";
import type { AlterationJob } from "../api";
import { AlterationLog } from "../components/alteration-results";
import { PlanStatusBadge } from "../components/plan-status-badge";
import { isPlanTerminal, useAlterationPlan } from "../hooks";
import { AlterationRoutes } from "../routes";

export function AlterationPlanPage({ params }: { params: RouteParams }) {
  const id = params.id;
  const query = useAlterationPlan(id);
  const plan = query.data?.plan;
  const jobs = query.data?.jobs ?? [];

  const columns: DataTableColumn<AlterationJob>[] = [
    { id: "instance", header: "Workflow instance", cell: (j) => <span className="font-mono text-xs">{j.workflowInstanceId}</span> },
    { id: "status", header: "Status", cell: (j) => <PlanStatusBadge status={j.status} /> },
    { id: "created", header: "Created", cell: (j) => <Timestamp value={j.createdAt} /> },
    { id: "started", header: "Started", cell: (j) => <Timestamp value={j.startedAt} /> },
    { id: "completed", header: "Completed", cell: (j) => <Timestamp value={j.completedAt} /> },
    { id: "log", header: "Log", cell: (j) => <AlterationLog entries={j.log} className="max-w-xl" /> },
  ];

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title={
          <span className="flex items-center gap-2">
            Plan <span className="font-mono text-base">{id}</span>
            <PlanStatusBadge status={plan?.status} />
          </span>
        }
        description={plan && !isPlanTerminal(plan.status) ? "Refreshing automatically until the plan completes." : undefined}
        actions={
          <>
            <Button variant="outline" size="sm" onClick={() => query.refetch()} disabled={query.isFetching}>
              <RefreshCw data-icon="inline-start" className={query.isFetching ? "animate-spin" : undefined} />
              Refresh
            </Button>
            <Button variant="ghost" size="sm" nativeButton={false} render={<Link href={AlterationRoutes.plans} />}>
              <ArrowLeft data-icon="inline-start" />
              All plans
            </Button>
          </>
        }
      />
      <ErrorAlert error={query.error} title="Could not load the plan" />
      {plan && (
        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Summary</CardTitle>
            </CardHeader>
            <CardContent>
              <KeyValueTable
                rows={[
                  { label: "Status", value: <PlanStatusBadge status={plan.status} /> },
                  { label: "Created", value: <Timestamp value={plan.createdAt} /> },
                  { label: "Started", value: <Timestamp value={plan.startedAt} /> },
                  { label: "Completed", value: <Timestamp value={plan.completedAt} /> },
                  { label: "Jobs", value: `${jobs.filter((j) => j.status === "Completed").length} / ${jobs.length} completed` },
                  { label: "Failed jobs", value: jobs.filter((j) => j.status === "Failed").length },
                ]}
              />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Alterations ({plan.alterations.length})</CardTitle>
            </CardHeader>
            <CardContent>
              <JsonViewer value={plan.alterations} maxHeight="16rem" />
            </CardContent>
          </Card>
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle>Workflow instance filter</CardTitle>
            </CardHeader>
            <CardContent>
              <JsonViewer value={plan.workflowInstanceFilter} maxHeight="12rem" />
            </CardContent>
          </Card>
        </div>
      )}
      <div>
        <h2 className="text-base font-semibold mb-2">Jobs</h2>
        <DataTable columns={columns} rows={jobs} rowKey={(j) => j.id} isLoading={query.isLoading} emptyMessage="No jobs generated yet." />
      </div>
    </div>
  );
}
