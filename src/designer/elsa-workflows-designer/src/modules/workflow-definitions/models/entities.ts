import {Activity, Variable, VersionedEntity} from "../../../models";

export interface WorkflowDefinition extends VersionedEntity {
  definitionId: string;
  name?: string;
  description?: string;
  variables?: Array<Variable>;
  metadata?: Map<string, any>;
  applicationProperties?: Map<string, any>;
  materializerName: string;
  materializerContext?: string;
  root: Activity;
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
