import 'reflect-metadata';
import {Container, Service} from "typedi";
import {ElsaClientProvider, EventBus, ServerSettings} from "../../services";
import {LoginResponse} from "./models";
import axios, {AxiosError, AxiosRequestConfig} from "axios";
import {EventTypes} from "../../models";

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
    const response = await httpClient.post<LoginResponse>(`identity/refresh-token`);
    return response.data;
  }
}
