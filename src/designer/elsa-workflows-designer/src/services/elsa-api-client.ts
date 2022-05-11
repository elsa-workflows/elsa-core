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
  VersionOptions,
  WorkflowDefinition, WorkflowInstance, WorkflowInstanceSummary, WorkflowStatus, WorkflowDefinitionSummary
} from '../models';
import 'reflect-metadata';
import {ServerSettings} from './server-settings';
import {getVersionOptionsString} from "../utils";

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
          const response = await httpClient.get<ActivityDescriptorResponse>('descriptors/activities');
          return response.data.activityDescriptors;
        }
      }
    },
    workflowDefinitions: {
      async publish(request: PublishWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
        const response = await httpClient.post<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/publish`);
        return response.data;
      },
      async retract(request: RetractWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
        const response = await httpClient.post<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/retract`);
        return response.data;
      },
      async delete(request: DeleteWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
        const response = await httpClient.delete<WorkflowDefinition>(`workflow-definitions/${request.definitionId}`);
        return response.data;
      },
      async post(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
        const response = await httpClient.post<WorkflowDefinition>('workflow-definitions', request);
        return response.data;
      },
      async get(request: GetWorkflowRequest): Promise<WorkflowDefinition> {
        const queryString = {};

        if (!!request.versionOptions)
          queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

        const queryStringText = serializeQueryString(queryString);
        const response = await httpClient.get<WorkflowDefinition>(`workflow-definitions/${request.definitionId}${queryStringText}`);
        return response.data;
      },
      async list(request: ListWorkflowDefinitionsRequest): Promise<PagedList<WorkflowDefinitionSummary>> {
        const queryString: any = {};

        if (!!request.materializerName)
          queryString.materializer = request.materializerName;

        if (!!request.versionOptions)
          queryString.versionOptions = getVersionOptionsString(request.versionOptions);

        if (!!request.page)
          queryString.page = request.page;

        if (!!request.pageSize)
          queryString.pageSize = request.pageSize;

        const queryStringText = serializeQueryString(queryString);
        const response = await httpClient.get<PagedList<WorkflowDefinitionSummary>>(`workflow-definitions${queryStringText}`);
        return response.data;
      },
      async export(request: ExportWorkflowRequest): Promise<ExportWorkflowResponse> {
        const queryString = {};

        if (!!request.versionOptions)
          queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

        const queryStringText = serializeQueryString(queryString);
        const definitionId = request.definitionId;

        const response = await httpClient.get(`workflow-definitions/${request.definitionId}/export${queryStringText}`, {
          responseType: "blob"
        });

        const contentDispositionHeader = response.headers["content-disposition"]; // Only available if the Elsa Server exposes the "Content-Disposition" header.
        const fileName = contentDispositionHeader ? contentDispositionHeader.split(";")[1].split("=")[1] : `workflow-definition-${definitionId}.json`;
        const data = response.data;

        return {
          fileName: fileName,
          data: data
        };
      },
      import: async (request: ImportWorkflowRequest): Promise<ImportWorkflowResponse> => {
        const file = request.file;
        const definitionId = request.definitionId;
        const json = await file.text();

        const response = await httpClient.post<WorkflowDefinition>(`workflow-definitions/${definitionId}/import`, json, {
          headers: {
            'Content-Type': 'application/json'
          }
        });

        const workflowDefinition = response.data;
        return {workflowDefinition: workflowDefinition};
      }
    },
    workflowInstances: {
      async list(request: ListWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>> {
        let queryString = {
          searchTerm: request.searchTerm,
          definitionId: request.definitionId,
          correlationId: request.correlationId,
          definitionIds: request.definitionIds.join(','),
          workflowStatus: request.workflowStatus,
          orderBy: request.orderBy,
          orderDirection: request.orderDirection,
          page: request.page,
          pageSize: request.pageSize
        };

        if (!!request.versionOptions)
          queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

        queryString['definitionIds'] = request.definitionIds.join(',');

        const queryStringText = serializeQueryString(queryString);
        const response = await httpClient.get<PagedList<WorkflowInstanceSummary>>(`workflow-instances${queryStringText}`);
        return response.data;
      },
      async get(request: GetWorkflowInstanceRequest): Promise<WorkflowInstance> {
        const response = await httpClient.get<WorkflowInstance>(`workflow-instances/${request.id}`);
        return response.data;
      },
      async delete(request: DeleteWorkflowInstanceRequest): Promise<WorkflowInstanceSummary> {
        const response = await httpClient.delete<WorkflowInstanceSummary>(`workflow-instances/${request.id}`);
        return response.data;
      },
      async deleteMany(request: BulkDeleteWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>> {
        const response = await httpClient.delete<PagedList<WorkflowInstanceSummary>>(`workflow-instances/bulk`);
        return response.data;
      }
    },
    designer: {
      runtimeSelectListApi: {
        get: async (providerTypeName: string, context?: any): Promise<SelectList> => {
          const response = await httpClient.post('designer/runtime-select-list', {
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
  workflowDefinitions: WorkflowDefinitionsApi;
  workflowInstances: WorkflowInstancesApi;
  designer: DesignerApi;
}

export interface DescriptorsApi {
  activities: ActivityDescriptorsApi;
}

export interface ActivityDescriptorsApi {
  list(): Promise<Array<ActivityDescriptor>>;
}

export interface WorkflowDefinitionsApi {
  post(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  get(request: GetWorkflowRequest): Promise<WorkflowDefinition>;

  list(request: ListWorkflowDefinitionsRequest): Promise<PagedList<WorkflowDefinitionSummary>>;

  delete(request: DeleteWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  retract(request: RetractWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  publish(request: PublishWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  export(request: ExportWorkflowRequest): Promise<ExportWorkflowResponse>;

  import(request: ImportWorkflowRequest): Promise<ImportWorkflowResponse>;
}

export interface WorkflowInstancesApi {

  list(request: ListWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>>;

  get(request: GetWorkflowInstanceRequest): Promise<WorkflowInstance>;

  delete(request: DeleteWorkflowInstanceRequest): Promise<WorkflowInstanceSummary>

  deleteMany(request: BulkDeleteWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>>
}

export interface DesignerApi {
  runtimeSelectListApi: RuntimeSelectListApi;
}

export interface RuntimeSelectListApi {
  get(providerTypeName: string, context?: any): Promise<SelectList>
}

export interface SaveWorkflowDefinitionRequest {
  definitionId: string;
  name?: string;
  description?: string;
  publish: boolean;
  root?: Activity
}

export interface DeleteWorkflowDefinitionRequest {
  definitionId: string;
}

export interface RetractWorkflowDefinitionRequest {
  definitionId: string;
}

export interface PublishWorkflowDefinitionRequest {
  definitionId: string;
}

export interface GetWorkflowRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportWorkflowRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportWorkflowResponse {
  fileName: string;
  data: Blob;
}

export interface ImportWorkflowRequest {
  definitionId: string;
  file: File;
}

export interface ImportWorkflowResponse {
  workflowDefinition: WorkflowDefinition;
}

export interface ListWorkflowDefinitionsRequest {
  page?: number;
  pageSize?: number;
  definitionIds?: Array<string>;
  versionOptions?: VersionOptions;
  materializerName?: string;
}

export interface ListWorkflowInstancesRequest {
  searchTerm?: string;
  definitionId?: string;
  correlationId?: string;
  definitionIds?: Array<string>;
  versionOptions?: VersionOptions;
  workflowStatus?: WorkflowStatus;
  orderBy?: OrderBy;
  orderDirection?: OrderDirection;
  page?: number;
  pageSize?: number;
}

export interface GetWorkflowInstanceRequest {
  id: string;
}

export interface DeleteWorkflowInstanceRequest {
  id: string;
}

export interface BulkDeleteWorkflowInstancesRequest {
  workflowInstanceIds: Array<string>;
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
