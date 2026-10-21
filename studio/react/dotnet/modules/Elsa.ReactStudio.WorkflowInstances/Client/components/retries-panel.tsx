
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { ApiError } from "@elsa-workflows/react-studio-api-client/http";
import type { ActivityExecutionRecord } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@elsa-workflows/react-studio-ui/table";

/** Choose an execution, not the graph's activity ID: a loop can execute the same node repeatedly. */
export function RetriesPanel({ executions }: { executions: ActivityExecutionRecord[] }) {
  const _t = useTranslation();
  const [selected, setSelected] = useState<string>();
  const execution = executions.find(item => item.id === selected) ?? executions[executions.length - 1];
  return <div className="flex flex-col gap-3">
    <SimpleSelect aria-label={_t("Retry activity execution")} value={execution?.id} disabled={executions.length === 0}
      options={executions.map(item => ({ value: item.id, label: _t("{{value0}} · {{value1}}", { value0: item.startedAt, value1: item.id }) }))} onChange={setSelected} />
    {execution ? <RetryAttempts key={execution.id} executionId={execution.id} workflowInstanceId={execution.workflowInstanceId} /> : <p>{_t("No activity executions yet.")}</p>}
  </div>;
}

function RetryAttempts({ executionId, workflowInstanceId }: { executionId: string; workflowInstanceId: string }) {
  const _t = useTranslation();
  const client = useElsaClient();
  const scope = useQueryScope();
  const [page, setPage] = useState(0);
  const [selected, setSelected] = useState<string>();
  const pageSize = 10;
  const query = useQuery({ queryKey: [scope, "workflow-instances", workflowInstanceId, "retries", executionId, page],
    queryFn: ({ signal }) => client.resilience.retries(executionId, { skip: page * pageSize, take: pageSize }, signal) });
  const unavailable = query.error instanceof ApiError && query.error.status === 404;
  const attempt = query.data?.items.find(item => item.id === selected);
  return <>
    {query.isLoading && <p role="status">{_t("Loading retry attempts…")}</p>}
    {unavailable ? <p>{_t("Retry history is unavailable on this server.")}</p> : <ErrorAlert error={query.error} title={_t("Could not load retry attempts")} />}
    {query.data && <>
      {query.data.items.length === 0 ? <p>{_t("No retry attempts for this execution.")}</p> : <Table>
        <TableHeader><TableRow><TableHead>{_t("Attempt")}</TableHead><TableHead>{_t("Delay")}</TableHead><TableHead>{_t("Details")}</TableHead></TableRow></TableHeader>
        <TableBody>{query.data.items.map(item => <TableRow key={item.id}>
          <TableCell>{item.attemptNumber}</TableCell><TableCell>{item.retryDelay}</TableCell>
          <TableCell><Button size="sm" variant="outline" aria-expanded={selected === item.id} onClick={() => setSelected(selected === item.id ? undefined : item.id)}>{_t("Details for attempt")}{" "}{item.attemptNumber}</Button></TableCell>
        </TableRow>)}</TableBody>
      </Table>}
      {attempt && <JsonViewer value={attempt.details} />}
    </>}
    {(query.data || page > 0) && <div className="flex items-center gap-2">
        <Button size="sm" variant="outline" disabled={page === 0 || query.isFetching} onClick={() => { setSelected(undefined); setPage(value => value - 1); }}>{_t("Previous attempts")}</Button>
        {query.data && <span className="text-sm">{query.data.totalCount} {_t("attempts")}</span>}
        <Button size="sm" variant="outline" disabled={query.isFetching || !query.data || (page + 1) * pageSize >= query.data.totalCount} onClick={() => { setSelected(undefined); setPage(value => value + 1); }}>{_t("Next attempts")}</Button>
    </div>}
  </>;
}
