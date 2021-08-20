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
    eventBus.on(EventTypes.FeatureUpdated, this.onWorkflowSettingsUpdated);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ConfigureFeature, this.onWorkflowSettingsEnabled);
    eventBus.detach(EventTypes.FeatureUpdated, this.onWorkflowSettingsUpdated);
  }
  
  onWorkflowSettingsEnabled(context: ConfigureFeatureContext) {
    if (context.featureName != "settings")
      return;

    context.headers.push({url: null, label: "Enabled", component: null, exact: false})
    context.columns.push({url: null, label: "Enabled", component: null, exact: false})
    context.hasContextItems = true;
  }  

  async onWorkflowSettingsUpdated(context: ConfigureFeatureContext) {
    let data: string[] = context.data;
    if (data[0] != "settings")
      return;

    const elsaClient = createElsaWorkflowSettingsClient(this.serverUrl);    
    this.workflowSettings = await elsaClient.workflowSettingsApi.list();

    const workflowBlueprintSettings = this.workflowSettings.find(x => x.workflowBlueprintId == data[1] && x.key == data[2]);
    if (workflowBlueprintSettings != undefined)
      await elsaClient.workflowSettingsApi.delete(workflowBlueprintSettings.id);
    
    const request: SaveWorkflowSettingsRequest = {
      workflowBlueprintId : data[1],
      key: data[2],
      value: data[3]
    };

    await elsaClient.workflowSettingsApi.save(request);

    eventBus.emit(EventTypes.WorkflowUpdated, this);
  }
}