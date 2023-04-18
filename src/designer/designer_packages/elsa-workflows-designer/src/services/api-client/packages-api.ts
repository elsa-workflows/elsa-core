import {Service} from "typedi";
import {ElsaClientProvider} from "./elsa-client";
import { PackageVersion } from "../../models";

@Service()
export class PackagesApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async getVersion(): Promise<PackageVersion> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<PackageVersion>(`package/version`);
    return response.data;
  }

  private getHttpClient = async () => await this.provider.getHttpClient();
}
