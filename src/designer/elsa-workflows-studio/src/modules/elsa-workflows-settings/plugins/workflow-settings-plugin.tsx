import {Component, Prop, State} from '@stencil/core';
import {createElsaWorkflowSettingsClient, SaveWorkflowSettingsRequest} from "../services/elsa-client";
import {eventBus} from "../../../services";
import {EventTypes, WorkflowSettingsEnabledContext, WorkflowSettingsUpdatedContext} from "../../../models";
import {WorkflowSettings} from "../models";

@Component({
    tag: 'elsa-workflow-settings-plugin',
    shadow: false,
})
export class ElsaWorkflowSettingsPlugin {
  @Prop() serverUrl: string;
  @State() workflowSettings: Array<WorkflowSettings>;

  connectedCallback() {
    eventBus.on(EventTypes.WorkflowSettingsEnabled, this.onWorkflowSettingsEnabled);
    eventBus.on(EventTypes.WorkflowSettingsUpdated, this.onWorkflowSettingsUpdated);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WorkflowSettingsEnabled, this.onWorkflowSettingsEnabled);
    eventBus.detach(EventTypes.WorkflowSettingsUpdated, this.onWorkflowSettingsUpdated);
  }

  onWorkflowSettingsEnabled(context: WorkflowSettingsEnabledContext) {
    context.isEnabled = true;
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