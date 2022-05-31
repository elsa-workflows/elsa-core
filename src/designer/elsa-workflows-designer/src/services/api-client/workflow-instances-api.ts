import {OrderBy, OrderDirection, PagedList, VersionOptions, WorkflowInstance, WorkflowInstanceSummary, WorkflowStatus, WorkflowSubStatus} from "../../models";
import {AxiosInstance} from "axios";
import {getVersionOptionsString, serializeQueryString} from "../../utils";

export interface WorkflowInstancesApi {

  list(request: ListWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>>;

  get(request: GetWorkflowInstanceRequest): Promise<WorkflowInstance>;

  delete(request: DeleteWorkflowInstanceRequest): Promise<WorkflowInstanceSummary>

  deleteMany(request: BulkDeleteWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>>
}

export interface ListWorkflowInstancesRequest {
  searchTerm?: string;
  definitionId?: string;
  correlationId?: string;
  definitionIds?: Array<string>;
  versionOptions?: VersionOptions;
  status?: WorkflowStatus;
  subStatus?: WorkflowSubStatus;
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

export class WorkflowInstancesApiImpl implements WorkflowInstancesApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(request: ListWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>> {
    let queryString = {
      searchTerm: request.searchTerm,
      definitionId: request.definitionId,
      correlationId: request.correlationId,
      status: request.status,
      subStatus: request.subStatus,
      orderBy: request.orderBy,
      orderDirection: request.orderDirection,
      page: request.page,
      pageSize: request.pageSize
    };

    if (!!request.versionOptions)
      queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    if (!!request.definitionIds)
      queryString['definitionIds'] = request.definitionIds.join(',');

    const queryStringText = serializeQueryString(queryString);
    const response = await this.httpClient.get<PagedList<WorkflowInstanceSummary>>(`workflow-instances${queryStringText}`);
    return response.data;
  }

  async get(request: GetWorkflowInstanceRequest): Promise<WorkflowInstance> {
    const response = await this.httpClient.get<WorkflowInstance>(`workflow-instances/${request.id}`);
    return response.data;
  }

  async delete(request: DeleteWorkflowInstanceRequest): Promise<WorkflowInstanceSummary> {
    const response = await this.httpClient.delete<WorkflowInstanceSummary>(`workflow-instances/${request.id}`);
    return response.data;
  }

  async deleteMany(request: BulkDeleteWorkflowInstancesRequest): Promise<PagedList<WorkflowInstanceSummary>> {
    const response = await this.httpClient.delete<PagedList<WorkflowInstanceSummary>>(`workflow-instances/bulk`);
    return response.data;
  }
}
