import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service} from 'axios-middleware';
import * as collection from 'lodash/collection';
import {eventBus} from '../../../services/event-bus';
import {ActivityModel, EventTypes, PagedList} from "../../../models";
import { Secret } from "../models/secret.model";


let _httpClient: AxiosInstance = null;
let _elsaSecretsClient: ElsaSecretsClient = null;

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

export const createElsaSecretsClient = async function (serverUrl: string): Promise<ElsaSecretsClient> {

  if (!!_elsaSecretsClient)
    return _elsaSecretsClient;

  const httpClient: AxiosInstance = await createHttpClient(serverUrl);

  _elsaSecretsClient = {
    secretsApi: {
      list: async () => {
        const response = await httpClient.get<Array<ActivityModel>>(`v1/secrets`);
        return response.data;
      },
      save: async request => {
        const response = await httpClient.post<Secret>('v1/secrets', request);
        return response.data;
      },
      delete: async id => {
        await httpClient.delete(`v1/secrets/${id}`);
      },
    }
  }

  return _elsaSecretsClient;
}

export interface ElsaSecretsClient {
  secretsApi: SecretsApi;
}

export interface SecretsApi {

  list(): Promise<Array<ActivityModel>>;

  save(request: ActivityModel): Promise<Secret>;

  delete(secretId: string): Promise<void>;
}

export interface SaveSecretsRequest {
  id?: string;
  workflowBlueprintId?: string;
  key?: string;
  value?: string;
}
