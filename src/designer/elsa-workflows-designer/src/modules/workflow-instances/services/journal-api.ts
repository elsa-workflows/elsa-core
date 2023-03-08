import {OrderBy, OrderDirection, PagedList, WorkflowExecutionLogRecord } from "../../../models";
import {serializeQueryString} from "../../../utils";
import {Service} from "typedi";
import {ElsaClientProvider} from "../../../services";

@Service()
export class JournalApi {

  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async list(request: GetWorkflowJournalRequest): Promise<PagedList<WorkflowExecutionLogRecord>> {
    let queryString = {
      page: request.page,
      pageSize: request.pageSize
    };

    const queryStringText = serializeQueryString(queryString);
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<PagedList<WorkflowExecutionLogRecord>>(`workflow-instances/${request.workflowInstanceId}/journal${queryStringText}`);
    return response.data;
  }

  async getLastEntry(request: GetLastEntryRequest): Promise<WorkflowExecutionLogRecord> {

    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<WorkflowExecutionLogRecord>(`workflow-instances/${request.workflowInstanceId}/journal/${request.activityId}`);
    return response.data;
  }

  private getHttpClient = async () => await this.provider.getHttpClient();
}

export interface GetWorkflowJournalRequest {
  workflowInstanceId: string;
  page?: number;
  pageSize?: number;
}

export interface GetLastEntryRequest {
  workflowInstanceId: string;
  activityId: string;
}
