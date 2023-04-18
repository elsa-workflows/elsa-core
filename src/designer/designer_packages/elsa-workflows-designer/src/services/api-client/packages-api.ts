import {Service} from "typedi";
import {ElsaClientProvider} from "./elsa-client";

@Service()
export class PackagesApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async getVersion(): Promise<string> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<string>(`package/version`);
    return response.data;
  }

  private getHttpClient = async () => await this.provider.getHttpClient();
}
