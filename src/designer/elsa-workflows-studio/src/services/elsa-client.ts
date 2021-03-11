﻿import axios, {AxiosRequestConfig} from "axios";
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
      },
      retract: async workflowDefinitionId => {
        const response = await httpClient.post<WorkflowDefinition>(`v1/workflow-definitions/${workflowDefinitionId}/retract`);
        return response.data;
      },
      export: async (workflowDefinitionId, versionOptions): Promise<ExportWorkflowResponse> => {
        const versionOptionsString = getVersionOptionsString(versionOptions);
        const response = await httpClient.post(`v1/workflow-definitions/${workflowDefinitionId}/${versionOptionsString}/export`, null, {
          responseType: 'blob'
        });

        const contentDispositionHeader = response.headers["content-disposition"]; // Only available if the Elsa Server exposes the "Content-Disposition" header.
        const fileName = contentDispositionHeader ? contentDispositionHeader.split(";")[1].split("=")[1] : `workflow-definition-${workflowDefinitionId}.json`;
        const data = response.data;

        return {
          fileName: fileName,
          data: data
        };
      }
    },
    scriptingApi: {
      getJavaScriptTypeDefinitions: async (workflowDefinitionId: string, context?: string): Promise<string> => {
        context = context || '';
        const response = await httpClient.get<string>(`v1/scripting/javascript/type-definitions/${workflowDefinitionId}?t=${new Date().getTime()}&context=${context}`);
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

  retract(workflowDefinitionId: string): Promise<WorkflowDefinition>

  export(workflowDefinitionId: string, versionOptions: VersionOptions): Promise<ExportWorkflowResponse>
}

export interface ScriptingApi {
  getJavaScriptTypeDefinitions(workflowDefinitionId: string, context?: string): Promise<string>
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
  publish?: boolean
  activities: Array<ActivityDefinition>
  connections: Array<ConnectionDefinition>
}

export interface ExportWorkflowResponse {
  fileName: string;
  data: Blob;
}
