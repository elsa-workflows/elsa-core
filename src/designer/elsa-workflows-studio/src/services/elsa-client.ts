import axios, {AxiosRequestConfig} from "axios";
import {ActivityDefinition, ActivityDescriptor, ConnectionDefinition, getVersionOptionsString, Variables, VersionOptions, WorkflowContextOptions, WorkflowDefinition, WorkflowPersistenceBehavior} from "../models";

export const createElsaClient = function (serverUrl: string): ElsaClient {
  const config: AxiosRequestConfig = {
    baseURL: serverUrl
  };

  const httpClient = axios.create(config);

  return {
    activitiesApi: {
      list: async () => {
        const response = await httpClient.get<Array<ActivityDescriptor>>('v1/activities');
        return response.data;
      }
    },
    workflowDefinitionsApi: {
      getByDefinitionAndVersion: async (definitionId: string, versionOptions: VersionOptions) => {
        const versionOptionsString = getVersionOptionsString(versionOptions);
        const response = await httpClient.get<WorkflowDefinition>(`v1/workflow-definitions/${definitionId}/${versionOptionsString}`);
        return response.data;
      },
      save: async request => {
        const response = await httpClient.post<WorkflowDefinition>('v1/workflow-definitions', request);
        return response.data;
      }
    }
  }
}

export interface ElsaClient {
  activitiesApi: ActivitiesApi
  workflowDefinitionsApi: WorkflowDefinitionsApi
}

export interface ActivitiesApi {
  list(): Promise<Array<ActivityDescriptor>>
}

export interface WorkflowDefinitionsApi {
  getByDefinitionAndVersion(definitionId: string, versionOptions: VersionOptions): Promise<WorkflowDefinition>
  save(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition>
}

export interface SaveWorkflowDefinitionRequest {
  workflowDefinitionId?: string
  name?: string
  displayName?: string
  description?: string
  variables?: Variables
  contextOptions?: WorkflowContextOptions
  isSingleton?: boolean
  persistenceBehavior?: WorkflowPersistenceBehavior
  deleteCompletedInstances?: boolean
  enabled?: boolean
  publish?: boolean
  activities: Array<ActivityDefinition>
  connections: Array<ConnectionDefinition>
}
