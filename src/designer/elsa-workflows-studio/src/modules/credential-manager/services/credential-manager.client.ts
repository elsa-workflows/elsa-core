import { AxiosInstance } from "axios";
import { createHttpClient } from '../../../services';
import { SecretModel } from "../models/secret.model";

let _elsaSecretsClient: ElsaSecretsClient = null;

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
        const response = await httpClient.post<SecretModel>('v1/secrets', request);
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

  save(request: SecretModel): Promise<SecretModel>;

  delete(secretId: string): Promise<void>;
}
