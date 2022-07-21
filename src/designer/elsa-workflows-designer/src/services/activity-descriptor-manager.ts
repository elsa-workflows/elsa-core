import 'reflect-metadata';
import {Container, Service} from "typedi";
import {ElsaApiClientProvider} from "./api-client/api-client";
import descriptorsStore from "../data/descriptors-store";

@Service()
export class ActivityDescriptorManager {
  private readonly elsaClientProvider: ElsaApiClientProvider;

  constructor() {
    this.elsaClientProvider = Container.get(ElsaApiClientProvider);
  }

  async refresh(): Promise<void> {
    const elsaClient = await this.elsaClientProvider.getElsaClient();
    descriptorsStore.activityDescriptors = await elsaClient.descriptors.activities.list();
  }
}
