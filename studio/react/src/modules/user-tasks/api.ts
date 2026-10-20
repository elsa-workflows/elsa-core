import type { HttpClient } from "@/lib/api/http";

/** Contract derived from `Elsa.UserTasks/Endpoints/UserTaskApiModels.cs` and `Models/UserTaskModels.cs`. */

export type UserTaskStatus =
  | "Unassigned"
  | "Available"
  | "Assigned"
  | "Completing"
  | "TimingOut"
  | "Cancelling"
  | "Completed"
  | "TimedOut"
  | "Cancelled";

export const UserTaskStatuses: UserTaskStatus[] = ["Unassigned", "Available", "Assigned", "Completing", "TimingOut", "Cancelling", "Completed", "TimedOut", "Cancelled"];

export type UserTaskScope = "assigned" | "available" | "history" | "all" | "needs-attention";
export type UserTaskDue = "overdue" | "today" | "week" | "none" | "";

export interface UserTaskParticipantSummary {
  kind: "user" | "group" | string;
  provider?: string | null;
  id: string;
  displayName?: string | null;
}

export interface UserTaskSummary {
  id: string;
  title: string;
  summary?: string | null;
  reference?: string | null;
  tags: string[];
  taskType?: string | null;
  status: UserTaskStatus;
  priority: number;
  assignee?: UserTaskParticipantSummary | null;
  candidateSummary?: string | null;
  dueAt?: string | null;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string;
  assignedAt?: string | null;
  completedAt?: string | null;
  workflowDefinitionId?: string | null;
  workflowDefinitionName?: string | null;
  workflowDefinitionVersion?: number | null;
  workflowInstanceId?: string | null;
  workflowInstanceReference?: string | null;
  healthSeverity?: "Advisory" | "Blocking" | string | null;
  healthCode?: string | null;
  allowedActions: string[];
  revision: number;
}

export interface UserTaskFormField {
  key: string;
  label: string;
  type: string;
  required: boolean;
  masked: boolean;
  canReveal: boolean;
  value?: unknown;
}

export interface UserTaskFormAction {
  key: string;
  label: string;
}

export interface UserTaskFormProjection {
  provider: string;
  key: string;
  version?: string | null;
  fields: UserTaskFormField[];
  actions: UserTaskFormAction[];
}

export interface UserTaskDetail extends UserTaskSummary {
  instructions?: string | null;
  data?: unknown;
  disclosure: { canViewProtected: boolean; canViewWorkflow: boolean; canViewHistory: boolean; guestVisible: boolean };
  workflow?: {
    definitionId?: string | null;
    definitionName?: string | null;
    definitionVersion?: number | null;
    instanceId?: string | null;
    instanceReference?: string | null;
  } | null;
  form?: UserTaskFormProjection | null;
  actions: UserTaskFormAction[];
  outcome?: string | null;
  response?: unknown;
  completedBy?: UserTaskParticipantSummary | null;
}

export interface UserTaskFeatureCapabilities {
  enabled: boolean;
  canList: boolean;
  canRead: boolean;
  canReadAll: boolean;
  canClaim: boolean;
  canComplete: boolean;
  canRelease: boolean;
  canAssign: boolean;
  canUpdate: boolean;
  canCancel: boolean;
  canCreateGuestLinks: boolean;
  canViewProtected: boolean;
  participantPicker: boolean;
  realtime: boolean;
  pollingIntervalSeconds: number;
}

export interface UserTaskCapabilities {
  taskId: string;
  revision: number;
  allowedActions: string[];
  canReadProtected: boolean;
  canManage: boolean;
}

export interface UserTaskEventSummary {
  id: string;
  kind: string;
  summary?: string | null;
  occurredAt: string;
  actorDisplayName?: string | null;
}

export interface ListUserTasksArgs {
  scope?: UserTaskScope;
  cursor?: string;
  limit?: number;
  sort?: "created" | "updated" | "due" | "priority" | string;
  direction?: "asc" | "desc";
  status?: UserTaskStatus[];
  priorityFrom?: number;
  priorityTo?: number;
  due?: UserTaskDue;
  from?: string;
  to?: string;
  workflowDefinitionId?: string;
  workflowInstanceId?: string;
  reference?: string;
  taskType?: string;
  search?: string;
  includeTotalCount?: boolean;
}

export interface UserTaskListResponse {
  items: UserTaskSummary[];
  nextCursor?: string | null;
  totalCount?: number | null;
}

export interface UserTaskMutationRequest {
  expectedRevision: number;
  operationId?: string;
  reason?: string;
}

export interface AssignUserTaskRequest extends UserTaskMutationRequest {
  assignee: UserTaskParticipantSummary;
}

export interface ScheduleUserTaskRequest extends UserTaskMutationRequest {
  priority?: number;
  dueAt?: string | null;
}

export interface CompleteUserTaskRequest {
  expectedRevision: number;
  operationId: string;
  actionKey: string;
  data?: unknown;
}

export interface CancelUserTaskRequest {
  expectedRevision: number;
  operationId: string;
  reason: string;
}

export interface UserTaskOperationResponse {
  operationId: string;
  status: "Accepted" | "Completed" | "Failed" | string;
  revision: number;
  task?: UserTaskSummary | null;
}

/** Error body for 409/422 responses (`UserTaskErrorResponse`). */
export interface UserTaskErrorResponse {
  code: string;
  message: string;
}

export interface UserTaskParticipantSearchResponse {
  items: UserTaskParticipantSummary[];
  nextCursor?: string | null;
}

export function userTasksApi(http: HttpClient) {
  const base = "user-tasks";
  const task = (id: string) => `${base}/${encodeURIComponent(id)}`;
  return {
    capabilities: (signal?: AbortSignal) => http.get<UserTaskFeatureCapabilities>(`${base}/capabilities`, { signal }),
    list: (args: ListUserTasksArgs, signal?: AbortSignal) => http.get<UserTaskListResponse>(base, { query: { ...args }, signal }),
    get: (id: string, signal?: AbortSignal) => http.get<UserTaskDetail>(task(id), { signal }),
    taskCapabilities: (id: string, signal?: AbortSignal) => http.get<UserTaskCapabilities>(`${task(id)}/capabilities`, { signal }),
    events: (id: string, cursor?: string, limit = 50, signal?: AbortSignal) =>
      http.get<{ items: UserTaskEventSummary[]; nextCursor?: string | null }>(`${task(id)}/events`, { query: { cursor, limit }, signal }),
    claim: (id: string, body: UserTaskMutationRequest) => http.post<UserTaskOperationResponse>(`${task(id)}/claim`, body),
    release: (id: string, body: UserTaskMutationRequest) => http.post<UserTaskOperationResponse>(`${task(id)}/release`, body),
    assign: (id: string, body: AssignUserTaskRequest) => http.post<UserTaskOperationResponse>(`${task(id)}/assign`, body),
    schedule: (id: string, body: ScheduleUserTaskRequest) => http.post<UserTaskOperationResponse>(`${task(id)}/schedule`, body),
    complete: (id: string, body: CompleteUserTaskRequest) => http.post<UserTaskOperationResponse>(`${task(id)}/complete`, body),
    cancel: (id: string, body: CancelUserTaskRequest) => http.post<UserTaskOperationResponse>(`${task(id)}/cancel`, body),
    retryResolution: (id: string, body: UserTaskMutationRequest) => http.post<UserTaskOperationResponse>(`${task(id)}/retry-resolution`, body),
    reveal: (id: string, fieldKey: string) => http.post<{ fieldKey: string; value?: unknown }>(`${task(id)}/reveal`, { fieldKey }),
    searchParticipants: (search: string, type?: "user" | "group", signal?: AbortSignal) =>
      http.get<UserTaskParticipantSearchResponse>("user-task-participants", { query: { search, type, limit: 20 }, signal }),
  };
}

export type UserTasksApi = ReturnType<typeof userTasksApi>;

export function participantLabel(p: UserTaskParticipantSummary | null | undefined): string {
  if (!p) return "";
  return p.displayName?.trim() || p.id;
}

export function isTerminalStatus(status: UserTaskStatus | string | undefined) {
  return status === "Completed" || status === "TimedOut" || status === "Cancelled";
}
