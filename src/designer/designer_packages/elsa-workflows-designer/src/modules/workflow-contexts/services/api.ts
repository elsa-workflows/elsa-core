import {Service} from "typedi";
import {ElsaClientProvider} from "../../../services";

@Service()
export class WorkflowContextsApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async list(): Promise<Array<WorkflowContextProviderDescriptor>> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<ListWorkflowContextsResponse>('workflow-contexts/provider-descriptors');
    return response.data.descriptors;
  }

  private getHttpClient = async () => await this.provider.getHttpClient();
}

export interface ListWorkflowContextsResponse {
  descriptors: Array<WorkflowContextProviderDescriptor>;
}

export interface WorkflowContextProviderDescriptor {
  name: string;
  type: string;
}
