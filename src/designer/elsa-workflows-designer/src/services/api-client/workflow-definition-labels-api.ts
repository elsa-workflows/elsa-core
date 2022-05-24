import {AxiosInstance} from "axios";
import {Label, List} from "../../models";

export interface WorkflowDefinitionLabelsApi {
  get(definitionId: string): Promise<Array<string>>;

  update(definitionId: string, labelIds: Array<string>): Promise<Array<string>>;
}

export class WorkflowDefinitionLabelsApiImpl implements WorkflowDefinitionLabelsApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async get(definitionVersionId: string): Promise<Array<string>> {
    const response = await this.httpClient.get<List<string>>(`workflow-definitions/${definitionVersionId}/labels`);
    return response.data.items;
  }

  async update(definitionVersionId: string, labelIds: Array<string>): Promise<Array<string>> {
    const response = await this.httpClient.post<List<string>>(`workflow-definitions/${definitionVersionId}/labels`, {labelIds: labelIds});
    return response.data.items;
  }

}
