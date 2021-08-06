import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service} from 'axios-middleware';
import * as collection from 'lodash/collection';
import {eventBus} from './event-bus';
import {
  ActivityDefinition,
  ActivityDescriptor,
  ConnectionDefinition,
  EventTypes,
  getVersionOptionsString, ListModel,
  OrderBy,
  PagedList,
  SelectListItem,
  Variables,
  VersionOptions,
  WorkflowBlueprint,
  WorkflowBlueprintSummary,
  WorkflowContextOptions,
  WorkflowDefinition,
  WorkflowDefinitionSummary,
  WorkflowExecutionLogRecord,
  WorkflowInstance,
  WorkflowInstanceSummary,
  WorkflowPersistenceBehavior,
  WorkflowStatus,
  WorkflowStorageDescriptor
} from "../models";
import {WebhookDefinition, WebhookDefinitionSummary} from "../models/webhook";

let _httpClient: AxiosInstance = null;
let _elsaClient: ElsaClient = null;

export const createHttpClient = function(baseAddress: string) : AxiosInstance
{
  if(!!_httpClient)
    return _httpClient;

  const config: AxiosRequestConfig = {
    baseURL: baseAddress
  };

  eventBus.emit(EventTypes.HttpClientConfigCreated, this, {config});

  const httpClient = axios.create(config);
  const service = new Service(httpClient);

  eventBus.emit(EventTypes.HttpClientCreated, this, {service, httpClient});
  
  return _httpClient = httpClient;
}

export const createElsaClient = function (serverUrl: string): ElsaClient {

  if (!!_elsaClient)
    return _elsaClient;

  const httpClient: AxiosInstance = createHttpClient(serverUrl);

  _elsaClient = {
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
      getMany: async (ids: Array<string>, versionOptions?: VersionOptions) => {
        const versionOptionsString = getVersionOptionsString(versionOptions);
        const response = await httpClient.get<ListModel<WorkflowDefinitionSummary>>(`v1/workflow-definitions?ids=${ids.join(',')}&version=${versionOptionsString}`);
        return response.data.items;
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
    webhookDefinitionsApi: {
      list: async (page?: number, pageSize?: number) => {
        const response = await httpClient.get<PagedList<WebhookDefinitionSummary>>(`v1/webhook-definitions`);
        return response.data;
      },
      getByWebhookId: async (webhookId: string) => {
        const response = await httpClient.get<WebhookDefinition>(`v1/webhook-definitions/${webhookId}`);
        return response.data;
      },
      save: async request => {
        const response = await httpClient.post<WebhookDefinition>('v1/webhook-definitions', request);
        return response.data;
      },
      update: async request => {
        const response = await httpClient.put<WebhookDefinition>('v1/webhook-definitions', request);
        return response.data;
      },
      delete: async webhookId => {
        await httpClient.delete(`v1/webhook-definitions/${webhookId}`);
      },
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
    workflowExecutionLogApi: {
      get: async (workflowInstanceId: string, page?: number, pageSize?: number): Promise<PagedList<WorkflowExecutionLogRecord>> => {
        const queryString = {};

        if (!!page)
          queryString['page'] = page;

        if (!!pageSize)
          queryString['pageSize'] = pageSize;

        const queryStringItems = collection.map(queryString, (v, k) => `${k}=${v}`);
        const queryStringText = queryStringItems.length > 0 ? `?${queryStringItems.join('&')}` : '';
        const response = await httpClient.get(`v1/workflow-instances/${workflowInstanceId}/execution-log${queryStringText}`);
        return response.data;
      }
    },
    scriptingApi: {
      getJavaScriptTypeDefinitions: async (workflowDefinitionId: string, context?: string): Promise<string> => {
        context = context || '';
        const response = await httpClient.get<string>(`v1/scripting/javascript/type-definitions/${workflowDefinitionId}?t=${new Date().getTime()}&context=${context}`);
        return response.data;
      }
    },
    designerApi: {
      runtimeSelectItemsApi: {
        get: async (providerTypeName: string, context?: any): Promise<Array<SelectListItem>> => {
          const response = await httpClient.post('v1/designer/runtime-select-list-items', {providerTypeName: providerTypeName, context: context});
          return response.data;
        }
      }
    },
    activityStatsApi: {
      get: async (workflowInstanceId: string, activityId?: any): Promise<ActivityStats> => {
        const response = await httpClient.get(`v1/workflow-instances/${workflowInstanceId}/activity-stats/${activityId}`);
        return response.data;
      }
    },
    workflowStorageProvidersApi: {
      list: async () => {
        const response = await httpClient.get<Array<WorkflowStorageDescriptor>>('v1/workflow-storage-providers');
        return response.data;
      }
    },
    workflowChannelsApi: {
      list: async () => {
        const response = await httpClient.get<Array<string>>('v1/workflow-channels');
        return response.data;
      }
    }
  }

  return _elsaClient;
}

export interface ElsaClient {
  activitiesApi: ActivitiesApi;
  workflowDefinitionsApi: WorkflowDefinitionsApi;
  workflowRegistryApi: WorkflowRegistryApi;
  workflowInstancesApi: WorkflowInstancesApi;
  workflowExecutionLogApi: WorkflowExecutionLogApi;
  scriptingApi: ScriptingApi;
  designerApi: DesignerApi;
  activityStatsApi: ActivityStatsApi;
  workflowStorageProvidersApi: WorkflowStorageProvidersApi;
  webhookDefinitionsApi: WebhookDefinitionsApi;
  workflowChannelsApi: WorkflowChannelsApi;
}

export interface ActivitiesApi {
  list(): Promise<Array<ActivityDescriptor>>;
}

export interface WorkflowDefinitionsApi {

  list(page?: number, pageSize?: number, versionOptions?: VersionOptions): Promise<PagedList<WorkflowDefinitionSummary>>;

  getMany(ids: Array<string>, versionOptions?: VersionOptions): Promise<Array<WorkflowDefinitionSummary>>;

  getByDefinitionAndVersion(definitionId: string, versionOptions: VersionOptions): Promise<WorkflowDefinition>;

  save(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  delete(definitionId: string): Promise<void>;

  retract(workflowDefinitionId: string): Promise<WorkflowDefinition>;

  export(workflowDefinitionId: string, versionOptions: VersionOptions): Promise<ExportWorkflowResponse>;

  import(workflowDefinitionId: string, file: File): Promise<WorkflowDefinition>;
}

export interface WebhookDefinitionsApi {

  list(page?: number, pageSize?: number): Promise<PagedList<WebhookDefinitionSummary>>;

  getByWebhookId(webhookId: string): Promise<WebhookDefinition>;

  save(request: SaveWebhookDefinitionRequest): Promise<WebhookDefinition>;

  update(request: SaveWebhookDefinitionRequest): Promise<WebhookDefinition>;

  delete(webhookId: string): Promise<void>;
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

export interface WorkflowExecutionLogApi {

  get(workflowInstanceId: string, page?: number, pageSize?: number): Promise<PagedList<WorkflowExecutionLogRecord>>;

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

export interface DesignerApi {
  runtimeSelectItemsApi: RuntimeSelectItemsApi;
}

export interface RuntimeSelectItemsApi {
  get(providerTypeName: string, context?: any): Promise<Array<SelectListItem>>
}

export interface ActivityStatsApi {
  get(workflowInstanceId: string, activityId: string): Promise<ActivityStats>;
}

export interface WorkflowStorageProvidersApi {
  list(): Promise<Array<WorkflowStorageDescriptor>>;
}

export interface WorkflowChannelsApi {
  list(): Promise<Array<string>>;
}

export interface SaveWorkflowDefinitionRequest {
  workflowDefinitionId?: string;
  name?: string;
  displayName?: string;
  description?: string;
  tag?: string;
  channel?: string;
  variables?: Variables;
  contextOptions?: WorkflowContextOptions;
  isSingleton?: boolean;
  persistenceBehavior?: WorkflowPersistenceBehavior;
  deleteCompletedInstances?: boolean;
  publish?: boolean;
  activities: Array<ActivityDefinition>;
  connections: Array<ConnectionDefinition>;
}

export interface SaveWebhookDefinitionRequest {
  id?: string;
  name?: string;
  path?: string;
  description?: string;
  payloadTypeName?: string;
  isEnabled?: boolean;
}

export interface ExportWorkflowResponse {
  fileName: string;
  data: Blob;
}

export interface ActivityStats {
  fault?: ActivityFault;
  averageExecutionTime: string;
  fastestExecutionTime: string;
  slowestExecutionTime: string;
  lastExecutedAt: Date;
  eventCounts: Array<ActivityEventCount>;
}

interface ActivityEventCount {
  eventName: string;
  count: number;
}

interface ActivityFault {
  message: string;
}