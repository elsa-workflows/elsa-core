import {Component, Prop, State, h} from '@stencil/core';
import {createElsaWorkflowSettingsClient, SaveWorkflowSettingsRequest} from "../services/elsa-client";
import {eventBus} from "../../../services";
import {EventTypes, ConfigureFeatureContext} from "../../../models";
import {WorkflowSettings} from "../models";

@Component({
    tag: 'elsa-workflow-settings-feature-plugin',
    shadow: false,
})
export class ElsaWorkflowSettingsFeaturePlugin {
  @Prop() serverUrl: string;  
  @State() workflowSettings: WorkflowSettings[];

  connectedCallback() {
    eventBus.on(EventTypes.ConfigureFeature, this.onWorkflowSettingsEnabled);
    eventBus.on(EventTypes.FeatureUpdating, this.onWorkflowSettingsUpdating);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ConfigureFeature, this.onWorkflowSettingsEnabled);
    eventBus.detach(EventTypes.FeatureUpdating, this.onWorkflowSettingsUpdating);
  }
  
  onWorkflowSettingsEnabled(context: ConfigureFeatureContext) {
    if (context.featureName != "settings")
      return;

    if (context.component != "ElsaWorkflowRegistryListScreen")
      return;

    const headers: any[] = [["Enabled"]];
    const hasContextItems: boolean = true;
  
    context.data = {headers, hasContextItems};
  }  

  async onWorkflowSettingsUpdating(context: ConfigureFeatureContext) {
    if (context.featureName != "settings")
      return;

    if (context.component != "ElsaWorkflowRegistryListScreen")
      return;

    const elsaClient = createElsaWorkflowSettingsClient(this.serverUrl);    
    this.workflowSettings = await elsaClient.workflowSettingsApi.list();

    const workflowBlueprintSettings = this.workflowSettings.find(x => x.workflowBlueprintId == context.params[0] && x.key == context.params[1]);
    if (workflowBlueprintSettings != undefined)
      await elsaClient.workflowSettingsApi.delete(workflowBlueprintSettings.id);
    
    const request: SaveWorkflowSettingsRequest = {
      workflowBlueprintId : context.params[0],
      key: context.params[1],
      value: context.params[2]
    };

    await elsaClient.workflowSettingsApi.save(request);
    eventBus.emit(EventTypes.FeatureUpdated, this);
  }
}