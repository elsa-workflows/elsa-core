﻿import {Map} from '../utils/utils';

export interface WorkflowDefinition {
    id?: string;
    definitionId?: string;
    tenantId?: string;
    name?: string;
    displayName?: string;
    description?: string;
    version: number;
    variables?: Variables;
    customAttributes?: Variables;
    contextOptions?: WorkflowContextOptions;
    isSingleton?: boolean;
    persistenceBehavior?: WorkflowPersistenceBehavior;
    deleteCompletedInstances?: boolean;
    isPublished?: boolean;
    isLatest?: boolean;
    activities: Array<ActivityDefinition>;
    connections: Array<ConnectionDefinition>;
}

export interface WorkflowDefinitionSummary {
    id?: string;
    definitionId?: string;
    tenantId?: string;
    name?: string;
    displayName?: string;
    description?: string;
    version: number;
    isSingleton?: boolean;
    persistenceBehavior?: WorkflowPersistenceBehavior;
    isPublished?: boolean;
    isLatest?: boolean;
}

export interface WorkflowBlueprintSummary {
    id: string;
    name?: string;
    displayName?: string;
    description?: string;
    version: number;
    tenantId?: string;
    isSingleton: boolean;
    isPublished: boolean;
    isLatest: boolean;
}

export interface WorkflowInstanceSummary
{
    id: string;
    definitionId: string;
    tenantId?: string;
    version: number;
    workflowStatus: WorkflowStatus;
    correlationId?: string;
    contextType?: string;
    contextId?: string;
    name?: string;
    createdAt?: Date
    lastExecutedAt?: Date;
    finishedAt?: Date;
    cancelledAt?: Date;
    faultedAt?: Date;
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

export interface ConnectionDefinitionMapped {
    sourceId: string;
    sourceActivityId: string;
    targetId: string;
    targetActivityId: string;
    outcome: string;
}

export interface ActivityDefinitionProperty {
    name: string;
    syntax?: string;
    expressions: Map<string>;
}

export interface Variables {
    data: Map<any>;
}

export interface WorkflowContextOptions {
    contextType: string;
    contextFidelity: WorkflowContextFidelity;
}

export enum WorkflowContextFidelity {
    Burst= 'Burst',
    Activity = 'Activity'
}

export enum WorkflowPersistenceBehavior {
    Suspended= 'Suspended',
    WorkflowBurst= ' WorkflowBurst',
    WorkflowPassCompleted = 'WorkflowPassCompleted',
    ActivityExecuted = 'ActivityExecuted'
}

export interface VersionOptions {
    isLatest?: boolean;
    isLatestOrPublished?: boolean;
    isPublished?: boolean;
    isDraft?: boolean;
    allVersions?: boolean;
    version?: number;
}

export enum WorkflowStatus {
    Idle= 'Idle',
    Running = 'Running',
    Finished = 'Finished',
    Suspended = 'Suspended',
    Faulted = 'Faulted',
    Cancelled = 'Cancelled'
}

export enum OrderBy {
    Started= 'Started',
    LastExecuted = 'LastExecuted',
    Finished = 'Finished'
}

export const getVersionOptionsString = (versionOptions?: VersionOptions) => {

    if (!versionOptions)
        return '';

    return versionOptions.allVersions
        ? 'AllVersions'
        : versionOptions.isDraft
            ? 'Draft'
            : versionOptions.isLatest
                ? 'Latest'
                : versionOptions.isPublished
                    ? 'Published'
                    : versionOptions.isLatestOrPublished
                        ? 'LatestOrPublished'
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
    uiHint: string;
    label?: string;
    hint?: string;
    options?: any;
    category?: string;
    defaultValue?: any;
    defaultSyntax?: string;
    supportedSyntaxes: Array<string>
}

export interface PagedList<T> {
    items: Array<T>;
    page: number;
    pageSize: number;
    totalCount: number;
}

export enum ActivityTraits {
    Action = 1,
    Trigger = 2,
    Job = 4
}

export class SyntaxNames {
    static readonly Literal = 'Literal';
    static readonly JavaScript = 'JavaScript';
    static readonly Liquid = 'Liquid';
    static readonly Json = 'Json';
    static Variable = 'Variable';
    static Output = 'Output';
}
