import {WorkflowState} from "./core";

export interface WorkflowDefinitionSummary {
  id: string;
  definitionId: string;
  version: number;
  name?: string;
  description?: string;
  isPublished: boolean;
  isLatest: boolean;
  materializerName: string;
}

export enum WorkflowStatus {
  Running = 'Running',
  Finished = 'Finished'
}

export enum WorkflowSubStatus {
  Executing = 'Executing',
  Suspended = 'Suspended',
  Finished = 'Finished',
  Compensating = 'Compensating',
  Cancelled = 'Cancelled',
  Faulted = 'Faulted',
}

export enum OrderBy {
  Created = 'Created',
  LastExecuted = 'LastExecuted',
  Finished = 'Finished'
}

export enum OrderDirection {
  Ascending = 'Ascending',
  Descending = 'Descending'
}

export interface WorkflowInstanceSummary {
  id: string;
  definitionId: string;
  definitionVersionId: string;
  version: number;
  status: WorkflowStatus;
  subStatus: WorkflowSubStatus;
  correlationId: string;
  name?: string;
  createdAt: Date;
  lastExecutedAt?: Date;
  finishedAt?: Date;
  cancelledAt?: Date;
  faultedAt?: Date;
}

export interface WorkflowInstance extends WorkflowInstanceSummary {
  workflowState: WorkflowState;
}

export interface PagedList<T> {
  items: Array<T>;
  page?: number;
  pageSize?: number;
  totalCount: number;
}

export interface List<T> {
  items: Array<T>;
}

export interface VersionOptions {
  isLatest?: boolean;
  isLatestOrPublished?: boolean;
  isPublished?: boolean;
  isDraft?: boolean;
  allVersions?: boolean;
  version?: number;
}

export interface IntellisenseContext {
  activityTypeName: string;
  propertyName: string;
}

export interface StorageDriverDescriptor {
  id: string;
  displayName: string;
}
