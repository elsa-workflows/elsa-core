import 'reflect-metadata';
import {Service} from "typedi";
import {ElsaClientProvider, ServerSettings} from "../../services";
import {LoginResponse} from "./models";
import axios, {AxiosRequestConfig} from "axios";

@Service()
export class LoginApi {

  constructor(private provider: ElsaClientProvider, private serverSettings: ServerSettings) {
    this.provider = provider;
  }

  async login(username: string, password: string): Promise<LoginResponse> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<LoginResponse>(`identity/login`, {username, password},);
    return response.data;
  }

  async refreshAccessToken(refreshToken: string): Promise<LoginResponse> {
    const config: AxiosRequestConfig = {
      baseURL: this.serverSettings.baseAddress,
      headers: {
        Authorization: `Bearer ${refreshToken}`
      }
    };

    const httpClient = axios.create(config);

    return await httpClient.post<LoginResponse>(`identity/refresh-token`)
      .then(response => response.data)
      .catch(error => error.response)
  }
}
