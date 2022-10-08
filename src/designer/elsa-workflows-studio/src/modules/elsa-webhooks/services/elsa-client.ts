import {AxiosInstance} from "axios";
import {PagedList} from "../../../models";
import {WebhookDefinition, WebhookDefinitionSummary} from "../models";
import {createHttpClient} from "../../../services/elsa-client"

let _elsaWebhooksClient: ElsaWebhooksClient = null;

export const createElsaWebhooksClient = async function (serverUrl: string): Promise<ElsaWebhooksClient> {

  if (!!_elsaWebhooksClient)
    return _elsaWebhooksClient;

  const httpClient: AxiosInstance = await createHttpClient(serverUrl);

  _elsaWebhooksClient = {
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
    }
  }

  return _elsaWebhooksClient;
}

export interface ElsaWebhooksClient {
  webhookDefinitionsApi: WebhookDefinitionsApi;
}

export interface WebhookDefinitionsApi {

  list(page?: number, pageSize?: number): Promise<PagedList<WebhookDefinitionSummary>>;

  getByWebhookId(webhookId: string): Promise<WebhookDefinition>;

  save(request: SaveWebhookDefinitionRequest): Promise<WebhookDefinition>;

  update(request: SaveWebhookDefinitionRequest): Promise<WebhookDefinition>;

  delete(webhookId: string): Promise<void>;
}

export interface SaveWebhookDefinitionRequest {
  id?: string;
  name?: string;
  path?: string;
  description?: string;
  payloadTypeName?: string;
  isEnabled?: boolean;
}
