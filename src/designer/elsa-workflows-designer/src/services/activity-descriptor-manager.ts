import 'reflect-metadata';
import {Container, Service} from "typedi";
import descriptorsStore from "../data/descriptors-store";
import {ElsaClientProvider} from "./api-client/elsa-client";

@Service()
export class ActivityDescriptorManager {
  private readonly elsaClientProvider: ElsaClientProvider;

  constructor() {
    this.elsaClientProvider = Container.get(ElsaClientProvider);
  }

  async refresh(): Promise<void> {
    const elsaClient = await this.elsaClientProvider.getElsaClient();
    descriptorsStore.activityDescriptors = await elsaClient.descriptors.activities.list();
  }
}
