import {List} from "../../models";
import {Label} from "./models";
import {ElsaClientProvider} from "../../services";
import {Service} from "typedi";

@Service()
export class LabelsApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async list(): Promise<Array<Label>> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.get<List<Label>>('labels');
    return response.data.items;
  }

  async create(name: string, description?: string, color?: string): Promise<Label> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<Label>('labels', {name, description, color});
    return response.data;
  }

  async update(id: string, name: string, description?: string, color?: string): Promise<Label> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<Label>(`labels/${id}`, {name, description, color});
    return response.data;
  }

  async delete(id: string): Promise<boolean> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.delete(`labels/${id}`);
    return response.status === 204;
  }
}

@Service()
export class WorkflowDefinitionLabelsApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async get(definitionVersionId: string): Promise<Array<string>> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.get<List<string>>(`workflow-definitions/${definitionVersionId}/labels`);
    return response.data.items;
  }

  async update(definitionVersionId: string, labelIds: Array<string>): Promise<Array<string>> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<List<string>>(`workflow-definitions/${definitionVersionId}/labels`, {labelIds: labelIds});
    return response.data.items;
  }

}
