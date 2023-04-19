import {Container, Service} from "typedi";
import {EventTypes, Plugin} from "../../models";
import {AuthContext, EventBus, InputControlRegistry} from "../../services";
import {h} from "@stencil/core";
import {TabModel, WorkflowPropertiesEditorDisplayingArgs, WorkflowPropertiesEditorEventTypes} from "../workflow-definitions/models/ui";
import {Widget} from "../workflow-instances/models";
import {WorkflowContextProviderDescriptor, WorkflowContextsApi} from "./services/api";
import descriptorsStore from "../../data/descriptors-store";

@Service()
export class WorkflowContextsPlugin implements Plugin {

  private apiClient: WorkflowContextsApi;
  private providerDescriptors: Array<WorkflowContextProviderDescriptor> = [];

  constructor() {
    this.apiClient = Container.get(WorkflowContextsApi);
    this.setupSignIn();
  }

  async initialize() {
    const authContext = Container.get(AuthContext);

    if (authContext.getIsSignedIn()) {
      await this.onSignedIn();
    }
  }

  private setupCustomInputControls() {
    const inputControlRegistry = Container.get(InputControlRegistry);
    const descriptors = this.providerDescriptors;
    inputControlRegistry.add('workflow-context-provider-picker', c => <elsa-workflow-context-provider-type-picker-input inputContext={c} descriptors={descriptors}/>)
  }

  private setupSignIn() {
    const eventBus = Container.get(EventBus);
    eventBus.on(EventTypes.Descriptors.Updated, this.onSignedIn);
  }

  private onSignedIn = async () => {
    // Need to do this post-sign in, this is a secure API call.
    const installedFeatures = descriptorsStore.installedFeatures;

    if(!installedFeatures.find(x => x.name == 'WorkflowContextsFeature'))
      return;

    this.providerDescriptors = await this.apiClient.list();
    this.setupCustomInputControls();
    this.setupCustomPropertyEditors();
  };

  private setupCustomPropertyEditors() {
    const eventBus = Container.get(EventBus);
    eventBus.detach(WorkflowPropertiesEditorEventTypes.Displaying, this.onPropertyPanelRendering)
    eventBus.on(WorkflowPropertiesEditorEventTypes.Displaying, this.onPropertyPanelRendering);
  }

  private onPropertyPanelRendering = (context: WorkflowPropertiesEditorDisplayingArgs) => {
    const tabs = context.model.tabModels;
    const workflowDefinition = context.workflowDefinition;
    const notifyWorkflowDefinitionChanged = context.notifyWorkflowDefinitionChanged;
    const descriptors = this.providerDescriptors;

    const contextProvidersWidget: Widget = {
      order: 20,
      name: 'workflow-context-providers',
      content: () => <elsa-workflow-context-provider-check-list descriptors={descriptors} workflowDefinition={workflowDefinition} onWorkflowDefinitionChanged={notifyWorkflowDefinitionChanged}/>
    };

    const workflowContextProvidersTab: TabModel = {
      name: 'workflow-context-providers',
      tab: {
        order: 20,
        displayText: 'Context',
        content: () => <elsa-widgets widgets={[contextProvidersWidget]}/>
      }
    };

    tabs.push(workflowContextProvidersTab);
  }
}
