export type WorkflowStatus = "Running" | "Finished";
export type WorkflowSubStatus = "Pending" | "Executing" | "Suspended" | "Finished" | "Faulted" | "Cancelled" | "Interrupted";
export type ActivityStatus = "Pending" | "Running" | "Completed" | "Canceled" | "Faulted";

export interface WorkflowInstanceSummary {
  id: string;
  definitionId: string;
  definitionVersionId: string;
  version: number;
  name?: string;
  status: WorkflowStatus;
  subStatus: WorkflowSubStatus;
  correlationId?: string;
  incidentCount: number;
  isSystem?: boolean;
  createdAt: string;
  updatedAt?: string;
  finishedAt?: string;
}

export interface WorkflowIncident {
  activityId: string;
  activityNodeId?: string;
  activityInstanceId?: string;
  activityType: string;
  message: string;
  exception?: { type: string; message: string; stackTrace?: string; innerException?: unknown };
  timestamp: string;
}

export interface Bookmark {
  id: string;
  name: string;
  hash: string;
  activityId: string;
  activityNodeId: string;
  activityInstanceId?: string;
  payload?: unknown;
  createdAt?: string;
}

export interface ActivityExecutionContextState {
  id: string;
  parentContextId?: string;
  scheduledActivityNodeId: string;
  ownerActivityNodeId?: string;
  status: ActivityStatus;
  properties: Record<string, unknown>;
  activityState: Record<string, unknown>;
  dynamicVariables: unknown[];
  startedAt?: string;
  completedAt?: string;
}

export interface WorkflowState {
  id: string;
  definitionId: string;
  definitionVersionId: string;
  definitionVersion: number;
  correlationId?: string;
  status: WorkflowStatus;
  subStatus: WorkflowSubStatus;
  bookmarks: Bookmark[];
  incidents: WorkflowIncident[];
  activityExecutionContexts: ActivityExecutionContextState[];
  input: Record<string, unknown>;
  output: Record<string, unknown>;
  properties: Record<string, unknown>;
  createdAt: string;
  updatedAt?: string;
  finishedAt?: string;
}

export interface WorkflowInstance extends WorkflowInstanceSummary {
  parentWorkflowInstanceId?: string;
  isExecuting?: boolean;
  workflowState: WorkflowState;
}

export interface WorkflowInstanceVariable {
  id: string;
  name: string;
  value: unknown;
}

export interface WorkflowExecutionState {
  status: WorkflowStatus;
  subStatus: WorkflowSubStatus;
  updatedAt: string;
}

export interface ListWorkflowInstancesArgs {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  definitionId?: string;
  definitionIds?: string[];
  definitionVersionId?: string;
  correlationId?: string;
  version?: number;
  status?: WorkflowStatus;
  statuses?: WorkflowStatus[];
  subStatus?: WorkflowSubStatus;
  subStatuses?: WorkflowSubStatus[];
  hasIncidents?: boolean;
  isSystem?: boolean;
  timestampFilters?: TimestampFilter[];
  orderBy?: "Created" | "UpdatedAt" | "Finished" | "Name";
  orderDirection?: "Ascending" | "Descending";
}

export interface TimestampFilter {
  column: "CreatedAt" | "UpdatedAt" | "FinishedAt";
  operator: "Is" | "IsNot" | "LessThan" | "GreaterThan" | "LessThanOrEqual" | "GreaterThanOrEqual";
  timestamp: string;
}

export interface WorkflowExecutionLogRecord {
  id: string;
  activityInstanceId: string;
  parentActivityInstanceId?: string;
  activityId: string;
  activityType: string;
  activityTypeVersion: number;
  activityName?: string;
  nodeId: string;
  timestamp: string;
  sequence: number;
  eventName: string;
  message?: string;
  source?: string;
  activityState?: Record<string, unknown>;
  payload?: Record<string, unknown>;
}

export interface ActivityExecutionRecord {
  id: string;
  workflowInstanceId: string;
  activityId: string;
  activityNodeId: string;
  activityType: string;
  activityTypeVersion: number;
  activityName?: string;
  activityState?: Record<string, unknown>;
  outputs?: Record<string, unknown>;
  payload?: Record<string, unknown>;
  properties?: Record<string, unknown>;
  exception?: { type: string; message: string; stackTrace?: string };
  status: ActivityStatus;
  hasBookmarks?: boolean;
  startedAt: string;
  completedAt?: string;
}

/** Aggregated execution stats per activity node, used to colour the read-only designer. */
export interface ActivityExecutionStats {
  activityNodeId: string;
  startedCount: number;
  completedCount: number;
  uncompletedCount: number;
  isBlocked: boolean;
  isFaulted: boolean;
}
