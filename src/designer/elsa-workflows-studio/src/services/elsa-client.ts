import axios, {AxiosRequestConfig} from "axios";
import * as collection from 'lodash/collection';
import {
  ActivityDefinition,
  ActivityDescriptor,
  ConnectionDefinition,
  getVersionOptionsString, OrderBy,
  PagedList,
  Variables,
  VersionOptions, WorkflowBlueprint, WorkflowBlueprintSummary,
  WorkflowContextOptions,
  WorkflowDefinition,
  WorkflowDefinitionSummary, WorkflowInstance, WorkflowInstanceSummary,
  WorkflowPersistenceBehavior, WorkflowStatus
} from "../models";

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
      list: async (page?: number, pageSize?: number, versionOptions?: VersionOptions) => {
        const versionOptionsString = getVersionOptionsString(versionOptions);
        const response = await httpClient.get<PagedList<WorkflowDefinitionSummary>>(`v1/workflow-definitions?version=${versionOptionsString}`);
        return response.data;
      },
      getByDefinitionAndVersion: async (definitionId: string, versionOptions: VersionOptions) => {
        const versionOptionsString = getVersionOptionsString(versionOptions);
        const response = await httpClient.get<WorkflowDefinition>(`v1/workflow-definitions/${definitionId}/${versionOptionsString}`);
        return response.data;
      },
      save: async request => {
        const response = await httpClient.post<WorkflowDefinition>('v1/workflow-definitions', request);
        return response.data;
      },
      delete: async definitionId => {
        await httpClient.delete(`v1/workflow-definitions/${definitionId}`);
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
      },
      import: async (workflowDefinitionId, file: File): Promise<WorkflowDefinition> => {
        const formData = new FormData();
        formData.append("file", file);
        const response = await httpClient.post<WorkflowDefinition>(`v1/workflow-definitions/${workflowDefinitionId}/import`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        return response.data;
      }
    },
    workflowRegistryApi: {
      list: async (page?: number, pageSize?: number, versionOptions?: VersionOptions): Promise<PagedList<WorkflowBlueprintSummary>> => {
        const versionOptionsString = getVersionOptionsString(versionOptions);
        const response = await httpClient.get<PagedList<WorkflowBlueprintSummary>>(`v1/workflow-registry?version=${versionOptionsString}`);
        return response.data;
      },

      get: async (id: string, versionOptions: VersionOptions) => {
        const versionOptionsString = getVersionOptionsString(versionOptions);
        const response = await httpClient.get<WorkflowBlueprint>(`v1/workflow-registry/${id}/${versionOptionsString}`);
        return response.data;
      }
    },
    workflowInstancesApi: {
      list: async (page?: number, pageSize?: number, workflowDefinitionId?: string, workflowStatus?: WorkflowStatus, orderBy?: OrderBy, searchTerm?: string): Promise<PagedList<WorkflowInstanceSummary>> => {
        const queryString = {};

        if (!!workflowDefinitionId)
          queryString['workflow'] = workflowDefinitionId;

        if (workflowStatus != null)
          queryString['status'] = workflowStatus;

        if (!!orderBy)
          queryString['orderBy'] = orderBy;

        if (!!searchTerm)
          queryString['searchTerm'] = searchTerm;

        if (!!page)
          queryString['page'] = page;

        if (!!pageSize)
          queryString['pageSize'] = pageSize;

        const queryStringItems = collection.map(queryString, (v, k) => `${k}=${v}`);
        const queryStringText = queryStringItems.length > 0 ? `?${queryStringItems.join('&')}` : '';
        const response = await httpClient.get<PagedList<WorkflowInstanceSummary>>(`v1/workflow-instances${queryStringText}`);
        return response.data;
      },
      get: async id => {
        const response = await httpClient.get(`v1/workflow-instances/${id}`);
        return response.data;
      },
      delete: async id => {
        await httpClient.delete(`v1/workflow-instances/${id}`);
      },
      bulkDelete: async request => {
        const response = await httpClient.delete(`v1/workflow-instances/bulk`, {
          data: request
        });
        return response.data;
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
  workflowRegistryApi: WorkflowRegistryApi;
  workflowInstancesApi: WorkflowInstancesApi;
  scriptingApi: ScriptingApi;
}

export interface ActivitiesApi {
  list(): Promise<Array<ActivityDescriptor>>;
}

export interface WorkflowDefinitionsApi {

  list(page?: number, pageSize?: number, versionOptions?: VersionOptions): Promise<PagedList<WorkflowDefinitionSummary>>;

  getByDefinitionAndVersion(definitionId: string, versionOptions: VersionOptions): Promise<WorkflowDefinition>;

  save(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  delete(definitionId: string): Promise<void>;

  retract(workflowDefinitionId: string): Promise<WorkflowDefinition>;

  export(workflowDefinitionId: string, versionOptions: VersionOptions): Promise<ExportWorkflowResponse>;

  import(workflowDefinitionId: string, file: File): Promise<WorkflowDefinition>;
}

export interface WorkflowRegistryApi {
  list(page?: number, pageSize?: number, versionOptions?: VersionOptions): Promise<PagedList<WorkflowBlueprintSummary>>;

  get(id: string, versionOptions: VersionOptions): Promise<WorkflowBlueprint>;
}

export interface WorkflowInstancesApi {
  list(page?: number, pageSize?: number, workflowDefinitionId?: string, workflowStatus?: WorkflowStatus, orderBy?: OrderBy, searchTerm?: string): Promise<PagedList<WorkflowInstanceSummary>>;

  get(id: string): Promise<WorkflowInstance>;

  delete(id: string): Promise<void>;

  bulkDelete(request: BulkDeleteWorkflowsRequest): Promise<BulkDeleteWorkflowsResponse>;
}

export interface BulkDeleteWorkflowsRequest {
  workflowInstanceIds: Array<string>;
}

export interface BulkDeleteWorkflowsResponse {
  deletedWorkflowCount: number;
}

export interface ScriptingApi {
  getJavaScriptTypeDefinitions(workflowDefinitionId: string, context?: string): Promise<string>
}

export interface SaveWorkflowDefinitionRequest {
  workflowDefinitionId?: string;
  name?: string;
  displayName?: string;
  description?: string;
  variables?: Variables;
  contextOptions?: WorkflowContextOptions;
  isSingleton?: boolean;
  persistenceBehavior?: WorkflowPersistenceBehavior;
  deleteCompletedInstances?: boolean;
  publish?: boolean;
  activities: Array<ActivityDefinition>;
  connections: Array<ConnectionDefinition>;
}

export interface ExportWorkflowResponse {
  fileName: string;
  data: Blob;
}
