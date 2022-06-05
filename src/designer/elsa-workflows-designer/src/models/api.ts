import {Type} from './shared';
import {WorkflowState} from "./core";

export interface ActivityDescriptor {
  activityType: string;
  displayName: string;
  category: string;
  description: string;
  inputProperties: Array<InputDescriptor>;
  outputProperties: Array<OutputDescriptor>;
  kind: ActivityKind;
  inPorts: Array<Port>;
  outPorts: Array<Port>;
}

export enum ActivityKind {
  Action = 'Action',
  Trigger = 'Trigger'
}

export interface PropertyDescriptor {
  name: string;
  type: Type;
  displayName?: string;
  description?: string;
  order?: number;
  isBrowsable?: boolean;
}

export interface InputDescriptor extends PropertyDescriptor {
  uiHint?: string;
  options?: any;
  category?: string;
  defaultValue?: any;
  defaultSyntax?: string;
  supportedSyntaxes?: Array<string>;
  isReadOnly?: boolean;
}

export interface OutputDescriptor extends PropertyDescriptor {
}

export interface Port {
  name: string;
  displayName: string;
}

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
