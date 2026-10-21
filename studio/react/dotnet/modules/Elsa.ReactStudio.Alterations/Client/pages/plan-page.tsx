"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ArrowLeft, RefreshCw } from "lucide-react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@elsa-workflows/react-studio-ui/card";
import { DataTable, type DataTableColumn } from "@elsa-workflows/react-studio-ui/common/data-table";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";
import { KeyValueTable } from "@elsa-workflows/react-studio-ui/common/key-value-table";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import type { RouteParams } from "@elsa-workflows/react-studio-sdk";
import type { AlterationJob } from "../api";
import { AlterationLog } from "../components/alteration-results";
import { PlanStatusBadge } from "../components/plan-status-badge";
import { isPlanTerminal, useAlterationPlan } from "../hooks";
import { AlterationRoutes } from "../routes";

export function AlterationPlanPage({ params }: { params: RouteParams }) {
  const _t = useTranslation();
  const id = params.id;
  const query = useAlterationPlan(id);
  const plan = query.data?.plan;
  const jobs = query.data?.jobs ?? [];

  const columns: DataTableColumn<AlterationJob>[] = [
    { id: "instance", header: translate("Workflow instance"), cell: (j) => <span className="font-mono text-xs">{j.workflowInstanceId}</span> },
    { id: "status", header: _t("Status"), cell: (j) => <PlanStatusBadge status={j.status} /> },
    { id: "created", header: _t("Created"), cell: (j) => <Timestamp value={j.createdAt} /> },
    { id: "started", header: _t("Started"), cell: (j) => <Timestamp value={j.startedAt} /> },
    { id: "completed", header: _t("Completed"), cell: (j) => <Timestamp value={j.completedAt} /> },
    { id: "log", header: _t("Log"), cell: (j) => <AlterationLog entries={j.log} className="max-w-xl" /> },
  ];

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title={
          <span className="flex items-center gap-2">
            {_t("Plan")}{" "}<span className="font-mono text-base">{id}</span>
            <PlanStatusBadge status={plan?.status} />
          </span>
        }
        description={plan && !isPlanTerminal(plan.status) ? _t("Refreshing automatically until the plan completes.") : undefined}
        actions={
          <>
            <Button variant="outline" size="sm" onClick={() => query.refetch()} disabled={query.isFetching}>
              <RefreshCw data-icon="inline-start" className={query.isFetching ? "animate-spin" : undefined} />
              {_t("Refresh")}</Button>
            <Button variant="ghost" size="sm" nativeButton={false} render={<Link href={AlterationRoutes.plans} />}>
              <ArrowLeft data-icon="inline-start" />
              {_t("All plans")}</Button>
          </>
        }
      />
      <ErrorAlert error={query.error} title={_t("Could not load the plan")} />
      {plan && (
        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>{_t("Summary")}</CardTitle>
            </CardHeader>
            <CardContent>
              <KeyValueTable
                rows={[
                  { label: _t("Status"), value: <PlanStatusBadge status={plan.status} /> },
                  { label: _t("Created"), value: <Timestamp value={plan.createdAt} /> },
                  { label: _t("Started"), value: <Timestamp value={plan.startedAt} /> },
                  { label: _t("Completed"), value: <Timestamp value={plan.completedAt} /> },
                  { label: _t("Jobs"), value: `${jobs.filter((j) => j.status === "Completed").length} / ${jobs.length} completed` },
                  { label: _t("Failed jobs"), value: jobs.filter((j) => j.status === "Failed").length },
                ]}
              />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>{_t("Alterations (")}{plan.alterations.length})</CardTitle>
            </CardHeader>
            <CardContent>
              <JsonViewer value={plan.alterations} maxHeight="16rem" />
            </CardContent>
          </Card>
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle>{_t("Workflow instance filter")}</CardTitle>
            </CardHeader>
            <CardContent>
              <JsonViewer value={plan.workflowInstanceFilter} maxHeight="12rem" />
            </CardContent>
          </Card>
        </div>
      )}
      <div>
        <h2 className="text-base font-semibold mb-2">{_t("Jobs")}</h2>
        <DataTable columns={columns} rows={jobs} rowKey={(j) => j.id} isLoading={query.isLoading} emptyMessage={_t("No jobs generated yet.")} />
      </div>
    </div>
  );
}
