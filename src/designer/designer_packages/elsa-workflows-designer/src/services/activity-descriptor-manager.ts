import 'reflect-metadata';
import {Container, Service} from "typedi";
import descriptorsStore from "../data/descriptors-store";
import {ElsaClientProvider} from "./api-client/elsa-client";
import { EventEmitter } from '@stencil/core';

@Service()
export class ActivityDescriptorManager {
  private readonly elsaClientProvider: ElsaClientProvider;
  private activityDescriptorsUpdatedCallback: (() => void) | null = null;

  constructor() {
    this.elsaClientProvider = Container.get(ElsaClientProvider);
  }

  onActivityDescriptorsUpdated(callback: () => void) {
    this.activityDescriptorsUpdatedCallback = callback;
  }

  async refresh(): Promise<void> {
    const elsaClient = await this.elsaClientProvider.getElsaClient();
    descriptorsStore.activityDescriptors = await elsaClient.descriptors.activities.list();

    if (this.activityDescriptorsUpdatedCallback) {
      this.activityDescriptorsUpdatedCallback();
    }
  }
}
