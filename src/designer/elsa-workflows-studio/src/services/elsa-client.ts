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
    },
    scriptingApi: {
      getJavaScriptTypeDefinitions: async (workflowDefinitionId: string): Promise<string> => {
        const response = await httpClient.get<string>(`v1/scripting/javascript/type-definitions/${workflowDefinitionId}?t=${new Date().getTime()}`);
        return response.data;
      }
    }
  }
}

export interface ElsaClient {
  activitiesApi: ActivitiesApi;
  workflowDefinitionsApi: WorkflowDefinitionsApi;
  scriptingApi: ScriptingApi;
}

export interface ActivitiesApi {
  list(): Promise<Array<ActivityDescriptor>>;
}

export interface WorkflowDefinitionsApi {
  getByDefinitionAndVersion(definitionId: string, versionOptions: VersionOptions): Promise<WorkflowDefinition>

  save(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition>
}

export interface ScriptingApi {
  getJavaScriptTypeDefinitions(workflowDefinitionId: string): Promise<string>
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
