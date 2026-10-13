import type { Activity, Variable } from "./activities";
import type { Link } from "./common";

export interface WorkflowOptions {
  activationStrategyType?: string;
  usableAsActivity?: boolean;
  autoUpdateConsumingWorkflows?: boolean;
  activityCategory?: string;
  incidentStrategyType?: string;
  commitStrategyName?: string;
  [key: string]: unknown;
}

export interface InputDefinition {
  name: string;
  type: string;
  displayName?: string;
  description?: string;
  category?: string;
  uiHint?: string;
  isArray?: boolean;
  storageDriverType?: string;
}

export interface OutputDefinition {
  name: string;
  type: string;
  displayName?: string;
  description?: string;
  isArray?: boolean;
}

/** Summary row as returned by the list endpoint. */
export interface WorkflowDefinitionSummary {
  id: string;
  definitionId: string;
  name?: string;
  description?: string;
  version: number;
  isLatest: boolean;
  isPublished: boolean;
  isReadonly?: boolean;
  isSystem?: boolean;
  materializerName?: string;
  providerName?: string;
  toolVersion?: string;
  createdAt: string;
  links?: Link[];
}

/** Full definition including the activity graph. */
export interface WorkflowDefinition extends WorkflowDefinitionSummary {
  variables: Variable[];
  inputs: InputDefinition[];
  outputs: OutputDefinition[];
  outcomes: string[];
  customProperties: Record<string, unknown>;
  options: WorkflowOptions;
  root: Activity;
  toolVersion?: string;
}

/** Payload of the save endpoint (`POST /workflow-definitions`). */
export interface WorkflowDefinitionModel {
  definitionId?: string;
  id?: string;
  name?: string;
  description?: string;
  version?: number;
  isLatest?: boolean;
  isPublished?: boolean;
  variables: Variable[];
  inputs: InputDefinition[];
  outputs: OutputDefinition[];
  outcomes: string[];
  customProperties: Record<string, unknown>;
  options: WorkflowOptions;
  root: Activity;
}

export interface SaveWorkflowDefinitionRequest {
  model: WorkflowDefinitionModel;
  publish?: boolean;
}

export interface SaveWorkflowDefinitionResponse {
  workflowDefinition: WorkflowDefinition;
  alreadyPublished?: boolean;
  consumingWorkflowCount?: number;
  validationErrors?: string[];
}

export interface ListWorkflowDefinitionsArgs {
  page?: number;
  pageSize?: number;
  versionOptions?: string;
  definitionIds?: string[];
  materializerName?: string;
  searchTerm?: string;
  isSystem?: boolean;
  orderBy?: "Name" | "Created" | "Version";
  orderDirection?: "Ascending" | "Descending";
  labels?: string[];
}

export interface ExecuteWorkflowDefinitionRequest {
  correlationId?: string;
  name?: string;
  input?: Record<string, unknown>;
  triggerActivityId?: string;
  versionOptions?: string;
}

export interface DispatchWorkflowDefinitionRequest extends ExecuteWorkflowDefinitionRequest {
  instanceId?: string;
}

export interface DispatchWorkflowDefinitionResponse {
  workflowInstanceId: string;
}

export interface BulkDeleteResponse {
  deleted: number;
}

export interface BulkPublishResponse {
  published: string[];
  alreadyPublished: string[];
  notFound: string[];
  skipped?: string[];
}

export interface BulkRetractResponse {
  retracted: string[];
  notPublished: string[];
  notFound: string[];
  skipped?: string[];
}
