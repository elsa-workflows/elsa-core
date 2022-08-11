import {Activity, Variable, VersionedEntity, VersionOptions} from "../../models";
import {TabModel} from "../workflow-definitions/models/ui";

export interface ActivityDefinition extends VersionedEntity {
  definitionId: string;
  type: string;
  displayName?: string;
  category?: string;
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
  type: string;
  displayName?: string;
  category?: string;
  description?: string;
  isPublished: boolean;
  isLatest: boolean;
}

export interface SaveActivityDefinitionRequest {
  definitionId: string;
  type: string;
  displayName?: string;
  category?: string;
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

export interface GetActivityDefinitionRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportActivityDefinitionRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportActivityDefinitionResponse {
  fileName: string;
  data: Blob;
}

export interface ImportActivityDefinitionRequest {
  definitionId: string;
  file: File;
}

export interface ImportActivityDefinitionResponse {
  activityDefinition: ActivityDefinition;
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

export interface ActivityDefinitionUpdatedArgs {
  activityDefinition: ActivityDefinition;
}

export interface ActivityDefinitionPropsUpdatedArgs {
  activityDefinition: ActivityDefinition;
}

export interface ActivityDefinitionPropertiesEditorModel {
  tabModels: Array<TabModel>;
}
