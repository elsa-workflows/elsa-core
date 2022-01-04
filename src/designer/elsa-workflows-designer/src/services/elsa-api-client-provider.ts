import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service as MiddlewareService} from 'axios-middleware';
import _ from 'lodash';
import {Container, Service} from 'typedi';
import {EventBus} from './event-bus';
import {
  Activity,
  ActivityDescriptor,
  ActivityDescriptorResponse,
  EventTypes,
  OrderBy,
  OrderDirection,
  PagedList,
  SelectList,
  Trigger,
  TriggerDescriptor,
  TriggerDescriptorResponse, VersionOptions,
  Workflow, WorkflowInstance, WorkflowInstanceSummary, WorkflowStatus, WorkflowSummary
} from '../models';
import 'reflect-metadata';
import {ServerSettings} from './server-settings';
import {getVersionOptionsString} from "../utils/utils";

function serializeQueryString(queryString: object): string {
  const filteredItems = _(queryString).omitBy(_.isUndefined).omitBy(_.isNull).value();
  const queryStringItems = _.map(filteredItems, (v, k) => `${k}=${v}`);
  return queryStringItems.length > 0 ? `?${queryStringItems.join('&')}` : '';
}

export async function createHttpClient(baseAddress: string): Promise<AxiosInstance> {
  const config: AxiosRequestConfig = {
    baseURL: baseAddress
  };

  const eventBus = Container.get(EventBus);
  await eventBus.emit(EventTypes.HttpClient.ConfigCreated, this, {config});

  const httpClient = axios.create(config);
  const service = new MiddlewareService(httpClient);

  await eventBus.emit(EventTypes.HttpClient.ClientCreated, this, {service, httpClient});

  return httpClient;
}

export async function createElsaClient(serverUrl: string): Promise<ElsaClient> {

  const httpClient: AxiosInstance = await createHttpClient(serverUrl);

  return {
    descriptors: {
      activities: {
        async list(): Promise<Array<ActivityDescriptor>> {
          const response = await httpClient.get<ActivityDescriptorResponse>('api/descriptors/activities');
          return response.data.activityDescriptors;
        }
      },
      triggers: {
        async list(): Promise<Array<TriggerDescriptor>> {
          const response = await httpClient.get<TriggerDescriptorResponse>('api/descriptors/triggers');
          return response.data.triggerDescriptors;
        }
      }
    },
    workflows: {
      async post(request: SaveWorkflowRequest): Promise<Workflow> {
        const response = await httpClient.post<Workflow>('api/workflows', request);
        return response.data;
      },
      async get(request: GetWorkflowRequest): Promise<Workflow> {
        const queryString = {};

        if (!!request.versionOptions)
          queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

        const queryStringText = serializeQueryString(queryString);
        const response = await httpClient.get<Workflow>(`api/workflows/${request.definitionId}${queryStringText}`);
        return response.data;
      },
      async list(request: ListWorkflowsRequest): Promise<PagedList<WorkflowSummary>> {
        const queryString = {};

        if (!!request.versionOptions)
          queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

        if (!!request.page)
          queryString['page'] = request.page;

        if (!!request.pageSize)
          queryString['pageSize'] = request.pageSize;

        const queryStringText = serializeQueryString(queryString);
        const response = await httpClient.get<PagedList<WorkflowSummary>>(`api/workflows${queryStringText}`);
        return response.data;
      },
      async getMany(request: GetManyWorkflowsRequest): Promise<Array<WorkflowSummary>> {
        const queryString = {};

        if (!!request.versionOptions)
          queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

        queryString['definitionIds'] = request.definitionIds.join(',');

        const queryStringText = serializeQueryString(queryString);
        const response = await httpClient.get<Array<WorkflowSummary>>(`api/workflows/set${queryStringText}`);
        return response.data;
      }
    },
    workflowInstances: {
      async list(request: ListWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>> {
        const queryStringText = serializeQueryString(request);
        const response = await httpClient.get<PagedList<WorkflowInstanceSummary>>(`api/workflow-instances${queryStringText}`);
        return response.data;
      },
      async get(request: GetWorkflowInstanceRequest): Promise<WorkflowInstance> {
        const response = await httpClient.get<WorkflowInstance>(`api/workflow-instances/${request.id}`);
        return response.data;
      }
    },
    designer: {
      runtimeSelectListApi: {
        get: async (providerTypeName: string, context?: any): Promise<SelectList> => {
          const response = await httpClient.post('api/designer/runtime-select-list', {
            providerTypeName: providerTypeName,
            context: context
          });
          return response.data;
        }
      }
    }
  };
}

export interface ElsaClient {
  descriptors: DescriptorsApi;
  workflows: WorkflowsApi;
  workflowInstances: WorkflowInstancesApi;
  designer: DesignerApi;
}

export interface DescriptorsApi {
  activities: ActivityDescriptorsApi;
  triggers: TriggerDescriptorsApi;
}

export interface ActivityDescriptorsApi {
  list(): Promise<Array<ActivityDescriptor>>;
}

export interface TriggerDescriptorsApi {
  list(): Promise<Array<TriggerDescriptor>>;
}

export interface WorkflowsApi {
  post(request: SaveWorkflowRequest): Promise<Workflow>;

  get(request: GetWorkflowRequest): Promise<Workflow>;

  list(request: ListWorkflowsRequest): Promise<PagedList<WorkflowSummary>>;

  getMany(request: GetManyWorkflowsRequest): Promise<Array<WorkflowSummary>>;
}

export interface WorkflowInstancesApi {

  list(request: ListWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>>;

  get(request: GetWorkflowInstanceRequest): Promise<WorkflowInstance>;
}

export interface DesignerApi {
  runtimeSelectListApi: RuntimeSelectListApi;
}

export interface RuntimeSelectListApi {
  get(providerTypeName: string, context?: any): Promise<SelectList>
}

export interface SaveWorkflowRequest {
  definitionId: string;
  name?: string;
  description?: string;
  triggers?: Array<Trigger>;
  publish: boolean;
  root?: Activity
}

export interface GetWorkflowRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ListWorkflowsRequest {
  page?: number;
  pageSize?: number;
  versionOptions?: VersionOptions;
}

export interface GetManyWorkflowsRequest {
  definitionIds?: Array<string>;
  versionOptions?: VersionOptions;
}

export interface ListWorkflowInstancesRequest {
  searchTerm?: string;
  definitionId?: string;
  correlationId?: string;
  version?: number;
  workflowStatus?: WorkflowStatus;
  orderBy?: OrderBy;
  orderDirection?: OrderDirection;
  page?: number;
  pageSize?: number;
}

export interface GetWorkflowInstanceRequest {
  id: string;
}

@Service()
export class ElsaApiClientProvider {
  private elsaClient: ElsaClient;

  constructor(private serverSettings: ServerSettings) {
  }

  public async getClient(): Promise<ElsaClient> {
    if (!!this.elsaClient)
      return this.elsaClient;

    return this.elsaClient = await createElsaClient(this.serverSettings.baseAddress);
  }
}
