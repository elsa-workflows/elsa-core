import {Activity, Type, Variable, VersionedEntity} from "../../../models";

export interface WorkflowDefinition extends VersionedEntity {
  definitionId: string;
  name?: string;
  description?: string;
  variables?: Array<Variable>;
  inputs?: Array<InputDefinition>;
  outputs?: Array<OutputDefinition>;
  outcomes?: Array<string>;
  customProperties?: Map<string, any>;
  materializerName: string;
  materializerContext?: string;
  usableAsActivity?: boolean;
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
  autoUpdateConsumingWorkflows?: boolean;
  activationStrategyType?: string;
}

export interface ArgumentDefinition {
  type: Type;
  isArray: boolean;
  name: string;
  displayName?: string;
  description?: string;
  category?: string;
}

export interface InputDefinition extends ArgumentDefinition {
  uiHint?: string;
  storageDriverType?: Type;
}

export interface OutputDefinition extends ArgumentDefinition {
}

