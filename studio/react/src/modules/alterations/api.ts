import type { HttpClient } from "@/lib/api/http";
import type { TimestampFilter, WorkflowStatus, WorkflowSubStatus } from "@/lib/api/types/workflow-instances";

/**
 * Alteration types registered by `Elsa.Alterations`. Serialized polymorphically with a `type`
 * discriminator equal to the CLR class name (see `AlterationSerializationOptionConfigurator`).
 */
export type Alteration =
  | { type: "Migrate"; targetVersion: number }
  | { type: "ModifyVariable"; variableId: string; value?: unknown }
  | { type: "ScheduleActivity"; activityId?: string; activityInstanceId?: string }
  | { type: "CancelActivity"; activityId?: string; activityInstanceId?: string }
  | { type: "Cancel" };

export type AlterationType = Alteration["type"];

export const AlterationTypes: { value: AlterationType; label: string; description: string }[] = [
  { value: "Migrate", label: "Migrate to version", description: "Move the instance to another published version of its definition." },
  { value: "ModifyVariable", label: "Modify variable", description: "Set a workflow variable to a new value." },
  { value: "ScheduleActivity", label: "Schedule activity", description: "Schedule an activity (by id or activity instance id) for execution." },
  { value: "CancelActivity", label: "Cancel activity", description: "Cancel a running activity (by id or activity instance id)." },
  { value: "Cancel", label: "Cancel workflow", description: "Cancel the whole workflow instance." },
];

/** Mirrors `AlterationWorkflowInstanceFilter`. */
export interface AlterationWorkflowInstanceFilter {
  emptyFilterSelectsAll?: boolean;
  workflowInstanceIds?: string[];
  correlationIds?: string[];
  names?: string[];
  searchTerm?: string;
  timestampFilters?: TimestampFilter[];
  definitionIds?: string[];
  definitionVersionIds?: string[];
  hasIncidents?: boolean;
  isSystem?: boolean | null;
  statuses?: WorkflowStatus[];
  subStatuses?: WorkflowSubStatus[];
}

/** Mirrors `AlterationPlanParams` (body of `POST /alterations/submit`). */
export interface AlterationPlanParams {
  id?: string;
  alterations: Alteration[];
  filter: AlterationWorkflowInstanceFilter;
}

export type AlterationPlanStatus = "Pending" | "Generating" | "Dispatching" | "Running" | "Completed" | "Failed";
export type AlterationJobStatus = "Pending" | "Running" | "Completed" | "Failed";

/** `Microsoft.Extensions.Logging.LogLevel`, either as its name or its numeric value. */
export type AlterationLogLevel = "Trace" | "Debug" | "Information" | "Warning" | "Error" | "Critical" | "None" | number;

export interface AlterationLogEntry {
  message: string;
  logLevel: AlterationLogLevel;
  timestamp: string;
  eventName?: string | null;
}

export interface AlterationPlan {
  id: string;
  alterations: Alteration[];
  workflowInstanceFilter: AlterationWorkflowInstanceFilter;
  status: AlterationPlanStatus;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
}

export interface AlterationJob {
  id: string;
  planId: string;
  workflowInstanceId: string;
  status: AlterationJobStatus;
  log?: AlterationLogEntry[] | null;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
}

export interface GetAlterationPlanResponse {
  plan: AlterationPlan;
  jobs: AlterationJob[];
}

export interface SubmitAlterationPlanResponse {
  planId: string;
}

export interface RunAlterationsRequest {
  alterations: Alteration[];
  workflowInstanceIds: string[];
}

export interface RunAlterationsResult {
  workflowInstanceId: string;
  log: { logEntries: AlterationLogEntry[] };
  workflowHasScheduledWork: boolean;
  isSuccessful: boolean;
}

export interface RunAlterationsResponse {
  results: RunAlterationsResult[];
}

export interface RetryWorkflowsRequest {
  workflowInstanceIds: string[];
  activityIds?: string[];
}

export interface DryRunResponse {
  workflowInstanceIds: string[];
}

const LEVEL_NAMES: Record<number, string> = { 0: "Trace", 1: "Debug", 2: "Information", 3: "Warning", 4: "Error", 5: "Critical", 6: "None" };

export function logLevelName(level: AlterationLogLevel): string {
  return typeof level === "number" ? (LEVEL_NAMES[level] ?? String(level)) : level;
}

export function isErrorLevel(level: AlterationLogLevel): boolean {
  const name = logLevelName(level);
  return name === "Error" || name === "Critical";
}

/** Endpoint group for `Elsa.Alterations`, built from the raw HTTP client. */
export function alterationsApi(http: HttpClient) {
  return {
    getPlan: (id: string, signal?: AbortSignal) =>
      http.get<GetAlterationPlanResponse>(`alterations/${encodeURIComponent(id)}`, { signal }),
    submit: (params: AlterationPlanParams) => http.post<SubmitAlterationPlanResponse>("alterations/submit", params),
    run: (request: RunAlterationsRequest) => http.post<RunAlterationsResponse>("alterations/run", request),
    dryRun: (filter: AlterationWorkflowInstanceFilter) => http.post<DryRunResponse>("alterations/dry-run", filter),
    retryWorkflows: (request: RetryWorkflowsRequest) => http.post<RunAlterationsResponse>("alterations/workflows/retry", request),
  };
}

export type AlterationsApi = ReturnType<typeof alterationsApi>;
