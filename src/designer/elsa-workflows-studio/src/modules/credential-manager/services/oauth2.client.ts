import { AxiosInstance } from "axios";
import { createHttpClient } from '../../../services';

let _elsaOauth2Client: ElsaOauth2Client = null;

export const createElsaOauth2Client = async function (serverUrl: string): Promise<ElsaOauth2Client> {

  if (!!_elsaOauth2Client)
    return _elsaOauth2Client;

  const httpClient: AxiosInstance = await createHttpClient(serverUrl);

  _elsaOauth2Client = {
    oauth2Api: {
      getUrl: async secretId => {
        const response = await httpClient.get<string>(`v1/oauth2/url/${secretId}`);
        return response.data;
      }
    }
  }

  return _elsaOauth2Client;
}

export interface ElsaOauth2Client {
  oauth2Api: Oauth2Api;
}

export interface Oauth2Api {
  getUrl(secretId: string): Promise<string>;
}
