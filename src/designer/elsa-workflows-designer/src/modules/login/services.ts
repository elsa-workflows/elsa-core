import 'reflect-metadata';
import {Service} from "typedi";
import {ElsaApiClientProvider} from "../../services";
import {LoginResponse} from "./models";
import {AxiosError} from "axios";

@Service()
export class LoginApi {
  private provider: ElsaApiClientProvider;

  constructor(provider: ElsaApiClientProvider) {
    this.provider = provider;
  }

  async login(username: string, password: string): Promise<LoginResponse> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<LoginResponse>(`identity/login`, {username, password},);
    return response.data;
  }
}
