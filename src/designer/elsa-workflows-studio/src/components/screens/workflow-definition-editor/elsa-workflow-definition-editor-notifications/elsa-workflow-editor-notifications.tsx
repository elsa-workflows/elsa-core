import {Component, Host, h} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import {EventTypes, WorkflowDefinition} from "../../../../models";

@Component({
  tag: 'elsa-workflow-definition-editor-notifications',
  shadow: false,
})
export class ElsaWorkflowEditorNotifications {

  toastNotificationElement: HTMLElsaToastNotificationElement;

  connectedCallback() {
    eventBus.on(EventTypes.WorkflowPublished, this.onWorkflowPublished);
    eventBus.on(EventTypes.WorkflowRetracted, this.onWorkflowRetracted);
    eventBus.on(EventTypes.WorkflowImported, this.onWorkflowImported);
  }

  disconnectedCallback() {
    eventBus.off(EventTypes.WorkflowPublished, this.onWorkflowPublished);
    eventBus.off(EventTypes.WorkflowRetracted, this.onWorkflowRetracted);
    eventBus.off(EventTypes.WorkflowImported, this.onWorkflowImported);
  }

  onWorkflowPublished = async (workflowDefinition: WorkflowDefinition) => await this.toastNotificationElement.show({
    autoCloseIn: 1500,
    title: 'Workflow Published',
    message: `Workflow successfully published at version ${workflowDefinition.version}.`
  });

  onWorkflowRetracted = async (workflowDefinition: WorkflowDefinition) => await this.toastNotificationElement.show({
    autoCloseIn: 1500,
    title: 'Workflow Retracted',
    message: `Workflow successfully retracted at version ${workflowDefinition.version}.`
  });

  onWorkflowImported = async (workflowDefinition: WorkflowDefinition) => await this.toastNotificationElement.show({autoCloseIn: 1500, title: 'Workflow Imported', message: `Workflow successfully imported.`});

  render() {

    return (
      <Host class="elsa-block">
        <elsa-toast-notification ref={el => this.toastNotificationElement = el}/>
      </Host>
    );
  }
}
