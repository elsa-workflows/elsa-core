import {Activity, Variable, VersionedEntity, VersionOptions} from "../../models";

export interface ActivityDefinition extends VersionedEntity {
  definitionId: string;
  name?: string;
  description?: string;
  createdAt?: Date;
  variables?: Array<Variable>;
  metadata?: Map<string, any>;
  applicationProperties?: Map<string, any>;
  root: Activity;
}

export interface ActivityDefinitionSummary {
  id: string;
  definitionId: string;
  version: number;
  name?: string;
  description?: string;
  isPublished: boolean;
  isLatest: boolean;
}

export interface SaveActivityDefinitionRequest {
  definitionId: string;
  name?: string;
  description?: string;
  publish: boolean;
  root?: Activity;
  variables?: Array<Variable>;
}

export interface BaseManyActivityDefinitionRequest {
  definitionIds: string[];
}

export interface DeleteManyActivityDefinitionRequest extends BaseManyActivityDefinitionRequest {
}

export interface PublishManyActivityDefinitionRequest extends BaseManyActivityDefinitionRequest {
}

export interface UnpublishManyActivityDefinitionRequest extends BaseManyActivityDefinitionRequest {
}

export interface DeleteActivityDefinitionRequest {
  definitionId: string;
}

export interface RetractActivityDefinitionRequest {
  definitionId: string;
}

export interface PublishActivityDefinitionRequest {
  definitionId: string;
}

export interface GetWorkflowRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportWorkflowRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportWorkflowResponse {
  fileName: string;
  data: Blob;
}

export interface ImportWorkflowRequest {
  definitionId: string;
  file: File;
}

export interface ImportWorkflowResponse {
  ActivityDefinition: ActivityDefinition;
}

export enum ActivityDefinitionsOrderBy {
  Name = 'Name',
  CreatedAt = 'CreatedAt',
}

export interface ListActivityDefinitionsRequest {
  page?: number;
  pageSize?: number;
  definitionIds?: Array<string>;
  versionOptions?: VersionOptions;
  materializerName?: string;
  orderBy?: ActivityDefinitionsOrderBy;
  label?: Array<string>;
}

export interface DeleteManyActivityDefinitionResponse {
  deleted: number;
}

export interface PublishManyActivityDefinitionResponse {
  published: string[];
  alreadyPublished: string[];
  notFound: string[];
}

export interface UnpublishManyActivityDefinitionResponse {
  retracted: string[];
  notPublished: string[];
  notFound: string[];
}
