import {Component, Prop, State, h} from '@stencil/core';
import {createElsaWorkflowSettingsClient, SaveWorkflowSettingsRequest} from "../services/elsa-client";
import {eventBus} from "../../../services";
import {EventTypes, ConfigureFeatureContext, WorkflowSettingsUpdatedContext} from "../../../models";
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
    //eventBus.on(EventTypes.WorkflowSettingsUpdated, this.onWorkflowSettingsUpdated);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ConfigureFeature, this.onWorkflowSettingsEnabled);
    //eventBus.detach(EventTypes.WorkflowSettingsUpdated, this.onWorkflowSettingsUpdated);
  }
  
  onWorkflowSettingsEnabled(context: ConfigureFeatureContext) {
    if (context.featureName != "settings")
      return;

    context.isEnabled = true;
    context.headers.push({url: null, label: "Enabled", component: null, exact: false})
    context.columns.push({url: null, label: "Enabled", component: null, exact: false})
    context.hasContextItems = true;
  }  

  async onWorkflowSettingsUpdated(context: WorkflowSettingsUpdatedContext) {

    const elsaClient = createElsaWorkflowSettingsClient(this.serverUrl);    
    this.workflowSettings = await elsaClient.workflowSettingsApi.list();

    const workflowBlueprintSettings = this.workflowSettings.find(x => x.workflowBlueprintId == context.workflowBlueprintId && x.key == context.key);
    if (workflowBlueprintSettings != undefined)
      await elsaClient.workflowSettingsApi.delete(workflowBlueprintSettings.id);

    const request: SaveWorkflowSettingsRequest = {
      workflowBlueprintId : context.workflowBlueprintId,
      key: context.key,
      value: context.value
    };

    await elsaClient.workflowSettingsApi.save(request);

    eventBus.emit(EventTypes.WorkflowUpdated, this);
  }
}