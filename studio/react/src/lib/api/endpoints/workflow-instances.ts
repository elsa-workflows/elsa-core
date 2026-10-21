import type { HttpClient } from "../http";
import type { ListResponse, PagedList } from "../types/common";
import type {
  ActivityExecutionRecord,
  ActivityExecutionStats,
  ListWorkflowInstancesArgs,
  WorkflowExecutionLogRecord,
  WorkflowExecutionState,
  WorkflowInstance,
  WorkflowInstanceSummary,
  WorkflowInstanceVariable,
} from "../types/workflow-instances";

export interface JournalFilter {
  activityIds?: string[];
  activityNodeIds?: string[];
  excludedActivityTypes?: string[];
  eventNames?: string[];
}

export function workflowInstancesApi(http: HttpClient) {
  return {
    /** The list endpoint accepts the same DTO as a JSON body on POST; used so array filters stay tidy. */
    list: (args: ListWorkflowInstancesArgs = {}, signal?: AbortSignal) =>
      http.post<PagedList<WorkflowInstanceSummary>>("workflow-instances", args, { signal }),

    get: (id: string, signal?: AbortSignal) =>
      http.get<WorkflowInstance>(`workflow-instances/${encodeURIComponent(id)}`, { signal }),

    delete: (id: string) => http.delete<void>(`workflow-instances/${encodeURIComponent(id)}`, { parse: "none" }),

    bulkDelete: (ids: string[]) => http.post<{ deleted: number }>("bulk-actions/delete/workflow-instances", { ids }),

    cancel: (id: string) => http.post<void>(`cancel/workflow-instances/${encodeURIComponent(id)}`, undefined, { parse: "none" }),

    bulkCancel: (request: { ids?: string[]; definitionVersionId?: string; definitionId?: string; versionOptions?: string }) =>
      http.post<{ cancelled: number }>("bulk-actions/cancel/workflow-instances/", request),

    journal: (id: string, args: { page?: number; pageSize?: number; filter?: JournalFilter } = {}, signal?: AbortSignal) =>
      http.post<PagedList<WorkflowExecutionLogRecord>>(
        `workflow-instances/${encodeURIComponent(id)}/journal`,
        { page: args.page, pageSize: args.pageSize, filter: args.filter },
        { signal },
      ),

    lastJournalEntry: (id: string, activityId: string) =>
      http.get<WorkflowExecutionLogRecord>(`workflow-instances/${encodeURIComponent(id)}/journal/${encodeURIComponent(activityId)}`),

    executionState: (id: string, signal?: AbortSignal) =>
      http.get<WorkflowExecutionState>(`workflow-instances/${encodeURIComponent(id)}/execution-state`, { signal }),

    variables: (id: string, signal?: AbortSignal) =>
      http.get<ListResponse<WorkflowInstanceVariable>>(`workflow-instances/${encodeURIComponent(id)}/variables`, { signal }),

    updateVariables: (id: string, variables: { id: string; value: unknown }[]) =>
      http.post<ListResponse<WorkflowInstanceVariable>>(`workflow-instances/${encodeURIComponent(id)}/variables`, { variables }),

    export: (id: string) => http.get<Blob>(`workflow-instances/${encodeURIComponent(id)}/export`, { parse: "blob" }),

    activityExecutions: (workflowInstanceId: string, activityNodeId?: string, signal?: AbortSignal) =>
      http.get<ListResponse<ActivityExecutionRecord>>("activity-executions/list", {
        query: { workflowInstanceId, activityNodeId },
        signal,
      }),

    activityExecution: (id: string) => http.get<ActivityExecutionRecord>(`activity-executions/${encodeURIComponent(id)}`),

    executionReport: (workflowInstanceId: string, activityNodeIds: string[], signal?: AbortSignal) =>
      http.post<{ stats: ActivityExecutionStats[] }>("activity-executions/report", { workflowInstanceId, activityNodeIds }, { signal }),
  };
}

export type WorkflowInstancesApi = ReturnType<typeof workflowInstancesApi>;
