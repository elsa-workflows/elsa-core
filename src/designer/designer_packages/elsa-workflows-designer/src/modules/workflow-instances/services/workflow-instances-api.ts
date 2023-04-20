import {OrderBy, OrderDirection, PagedList, VersionOptions, WorkflowExecutionLogRecord, WorkflowInstance, WorkflowInstanceSummary, WorkflowStatus, WorkflowSubStatus} from "../../../models";
import {getVersionOptionsString, serializeQueryString} from "../../../utils";
import {Service} from "typedi";
import {ElsaClientProvider} from "../../../services";

@Service()
export class WorkflowInstancesApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
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
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<PagedList<WorkflowInstanceSummary>>(`workflow-instances${queryStringText}`);
    return response.data;
  }

  async get(request: GetWorkflowInstanceRequest): Promise<WorkflowInstance> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<WorkflowInstance>(`workflow-instances/${request.id}`);
    return response.data;
  }

  async delete(request: DeleteWorkflowInstanceRequest): Promise<WorkflowInstanceSummary> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.delete<WorkflowInstanceSummary>(`workflow-instances/${request.id}`);
    return response.data;
  }

  async deleteMany(request: BulkDeleteWorkflowInstancesRequest): Promise<number> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<number>(`bulk-actions/delete/workflow-instances/by-id`, {
      Ids: request.workflowInstanceIds,
    });
    return response.data;
  }

  async cancelMany(request: BulkCancelWorkflowInstancesRequest): Promise<number> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<number>(`bulk-actions/cancel/workflow-instances/by-id`, {
      Ids: request.workflowInstanceIds,
    });
    return response.data;
  }

  async getJournal(request: GetWorkflowJournalRequest): Promise<PagedList<WorkflowExecutionLogRecord>> {
    let queryString = {
      page: request.page,
      pageSize: request.pageSize
    };

    const queryStringText = serializeQueryString(queryString);
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<PagedList<WorkflowExecutionLogRecord>>(`workflow-instances/${request.workflowInstanceId}/journal${queryStringText}`);
    return response.data;
  }

  private getHttpClient = async () => await this.provider.getHttpClient();
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

export interface BulkCancelWorkflowInstancesRequest {
  workflowInstanceIds: Array<string>;
}

export interface GetWorkflowJournalRequest {
  workflowInstanceId: string;
  page?: number;
  pageSize?: number;
}