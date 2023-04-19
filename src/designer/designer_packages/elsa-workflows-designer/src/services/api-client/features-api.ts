import {Service} from "typedi";
import {ElsaClientProvider} from "./elsa-client";
import { PackageVersion } from "../../models";
import {AxiosInstance} from "axios";

@Service()
export class FeaturesApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async getInstalledFeatures(): Promise<Array<FeatureDescriptor>> {
    const response = await this.httpClient.get<Response>(`features/installed`);
    return response.data.installedFeatures;
  }
}

export interface FeatureDescriptor {
  name: string;
  displayName: string;
  description: string;
}

interface Response {
  installedFeatures: Array<FeatureDescriptor>;
}
