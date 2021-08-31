import {Component, Prop, State, h} from '@stencil/core';
import {createElsaWorkflowSettingsClient, SaveWorkflowSettingsRequest} from "../services/elsa-client";
import {eventBus, ElsaPlugin} from "../../../services";
import {EventTypes, ConfigureWorkflowRegistryColumnsContext, ConfigureWorkflowRegistryUpdatingContext} from "../../../models";
import {WorkflowSettings} from "../models";

@Component({
    tag: 'elsa-workflow-settings-plugin',
    shadow: false,
})
export class WorkflowSettingsPlugin implements ElsaPlugin {
  @Prop() serverUrl: string;  
  @State() workflowSettings: WorkflowSettings[];

  connectedCallback() {
    eventBus.on(EventTypes.WorkflowRegistryLoadingColumns, this.onLoadingColumns);
    eventBus.on(EventTypes.WorkflowRegistryUpdating, this.onUpdating);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WorkflowRegistryLoadingColumns, this.onLoadingColumns);
    eventBus.detach(EventTypes.WorkflowRegistryUpdating, this.onUpdating);
  }
  
  onLoadingColumns(context: ConfigureWorkflowRegistryColumnsContext) {

    const headers: any[] = [["Enabled"]];
    const hasContextItems: boolean = true;
  
    context.data = {headers, hasContextItems};
  }  

  async onUpdating(context: ConfigureWorkflowRegistryUpdatingContext) {

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
    eventBus.emit(EventTypes.WorkflowRegistryUpdated, this);
  }
}