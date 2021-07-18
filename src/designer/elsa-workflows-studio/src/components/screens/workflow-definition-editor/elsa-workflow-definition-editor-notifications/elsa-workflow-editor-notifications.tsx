import {Component, Host, h} from '@stencil/core';
import {eventBus} from '../../../../services';
import {EventTypes, WorkflowDefinition} from "../../../../models";
import {toastNotificationService} from "../../../../services/toast-notification-service";

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
    eventBus.off(EventTypes.WorkflowPublished);
    eventBus.off(EventTypes.WorkflowRetracted);
    eventBus.off(EventTypes.WorkflowImported);
  }

  onWorkflowPublished = (workflowDefinition: WorkflowDefinition) => toastNotificationService.show('Workflow Published', `Workflow successfully published at version ${workflowDefinition.version}.`, 1500);
  onWorkflowRetracted = (workflowDefinition: WorkflowDefinition) => toastNotificationService.show('Workflow Retracted', `Workflow successfully retracted at version ${workflowDefinition.version}.`, 1500);
  onWorkflowImported = (workflowDefinition: WorkflowDefinition) => toastNotificationService.show('Workflow Imported', `Workflow successfully imported.`, 1500);
}
