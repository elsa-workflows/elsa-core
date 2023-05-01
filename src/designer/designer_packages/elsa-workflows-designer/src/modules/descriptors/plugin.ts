import 'reflect-metadata';
import {Service, Token} from "typedi";
import {EventTypes, Plugin} from "../../models";
import {AuthContext, ElsaClientProvider, EventBus} from "../../services";
import descriptorsStore from "../../data/descriptors-store";

@Service()
export class DescriptorsPlugin implements Plugin {

  constructor(private eventBus: EventBus, private elsaClientProvider: ElsaClientProvider, private authContext: AuthContext) {
  }

  async initialize(): Promise<void> {
    this.eventBus.on(EventTypes.Auth.SignedIn, async () => await this.loadDescriptors());
  }

  private loadDescriptors = async (): Promise<void> => {
    const elsaClient = await this.elsaClientProvider.getElsaClient();
    const activityDescriptors = await elsaClient.descriptors.activities.list();
    const storageDrivers = await elsaClient.descriptors.storageDrivers.list();
    const variableDescriptors = await elsaClient.descriptors.variables.list();
    const workflowInstantiationStrategyDescriptors = await elsaClient.descriptors.workflowActivationStrategies.list();
    const installedFeatures = await elsaClient.descriptors.features.getInstalledFeatures();

    descriptorsStore.activityDescriptors = activityDescriptors;
    descriptorsStore.storageDrivers = storageDrivers;
    descriptorsStore.variableDescriptors = variableDescriptors;
    descriptorsStore.workflowActivationStrategyDescriptors = workflowInstantiationStrategyDescriptors;
    descriptorsStore.installedFeatures = installedFeatures;

    await this.eventBus.emit(EventTypes.Descriptors.Updated, this);
  };
}
