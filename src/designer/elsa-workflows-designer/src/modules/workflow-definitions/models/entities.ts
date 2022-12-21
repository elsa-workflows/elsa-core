import {Activity, Variable, VersionedEntity} from "../../../models";

export interface WorkflowDefinition extends VersionedEntity {
  definitionId: string;
  name?: string;
  description?: string;
  variables?: Array<Variable>;
  customProperties?: Map<string, any>;
  materializerName: string;
  materializerContext?: string;
  options?: WorkflowOptions;
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


export interface WorkflowOptions {
  instantiationStrategyType?: string;
}
