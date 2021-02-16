import {Map} from '../utils/utils';

export interface WorkflowDefinition {
  id?: string,
  definitionId?: string,
  tenantId?: string,
  name?: string,
  displayName?: string,
  description?: string,
  version: number,
  variables?: Variables,
  customAttributes?: Variables,
  contextOptions?: WorkflowContextOptions,
  isSingleton?: boolean,
  persistenceBehavior?: WorkflowPersistenceBehavior,
  deleteCompletedInstances?: boolean,
  isEnabled?: boolean,
  isPublished?: boolean,
  isLatest?: boolean,
  activities: Array<ActivityDefinition>,
  connections: Array<ConnectionDefinition>
}

export interface ActivityDefinition {
  activityId: string;
  type: string;
  name: string;
  displayName: string;
  description: string;
  persistWorkflow: boolean;
  loadWorkflowContext: boolean;
  saveWorkflowContext: boolean;
  persistOutput: boolean;
  properties: Array<ActivityDefinitionProperty>;
}

export interface ConnectionDefinition {
  sourceActivityId?: string;
  targetActivityId?: string;
  outcome?: string;
}

export interface ActivityDefinitionProperty {
  name: string;
  syntax: string;
  expression: string;
}

export interface Variables {
  data: Map<object>;
}

export interface WorkflowContextOptions {
  contextType: string;
  contextFidelity: WorkflowContextFidelity;
}

export enum WorkflowContextFidelity {
  Burst,
  Activity
}

export enum WorkflowPersistenceBehavior {
  Suspended,
  WorkflowPAssCompleted,
  ActivityExecuted
}

export interface VersionOptions {
  isLatest?: boolean;
  isLatestOrPublished?: boolean;
  isPublished?: boolean;
  isDraft?: boolean;
  allVersions?: boolean;
  version?: number;
}

export const getVersionOptionsString = (versionOptions: VersionOptions) => {
  return versionOptions.allVersions
    ? "AllVersions"
    : versionOptions.isDraft
      ? "Draft"
      : versionOptions.isLatest
        ? "Latest"
        : versionOptions.isPublished
          ? "Published"
          : versionOptions.isLatestOrPublished
            ? "LatestOrPublished"
            : versionOptions.version.toString();
};

export interface ActivityDescriptor {
  type: string;
  displayName: string;
  description?: string;
  category: string;
  traits: ActivityTraits;
  outcomes: Array<string>;
  browsable: boolean;
  properties: Array<ActivityPropertyDescriptor>;
}

export interface ActivityPropertyDescriptor {
  name: string;
  type: string;
  label?: string;
  hint?: string;
  options?: any;
}

export enum ActivityTraits {
  Action = 1,
  Trigger = 2,
  Job = 4
}
