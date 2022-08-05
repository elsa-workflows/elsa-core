import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service} from 'axios-middleware';
import {eventBus} from '../../../services';
import { EventTypes } from "../../../models";
import { Secret, SecretModel } from "../models/secret.model";


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
        const response = await httpClient.get<Array<SecretModel>>(`v1/secrets`);
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

  list(): Promise<Array<SecretModel>>;

  save(request: SecretModel): Promise<Secret>;

  delete(secretId: string): Promise<void>;
}
