import {Component, Host, h} from '@stencil/core';
import {eventBus, toastNotificationService} from '../../../../services';
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
    eventBus.on(EventTypes.ClipboardPermissionDenied, this.onClipboardPermissionsDenied);
    eventBus.on(EventTypes.ClipboardCopied, this.onClipboardCopied);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WorkflowPublished, this.onWorkflowPublished);
    eventBus.detach(EventTypes.WorkflowRetracted, this.onWorkflowRetracted);
    eventBus.detach(EventTypes.WorkflowImported, this.onWorkflowImported);
    eventBus.detach(EventTypes.ClipboardPermissionDenied, this.onClipboardPermissionsDenied);
    eventBus.detach(EventTypes.ClipboardCopied, this.onClipboardCopied);
  }

  onWorkflowPublished = (workflowDefinition: WorkflowDefinition) => toastNotificationService.show('Workflow Published', `Workflow successfully published at version ${workflowDefinition.version}.`, 1500);
  onWorkflowRetracted = (workflowDefinition: WorkflowDefinition) => toastNotificationService.show('Workflow Retracted', `Workflow successfully retracted at version ${workflowDefinition.version}.`, 1500);
  onWorkflowImported = () => toastNotificationService.show('Workflow Imported', `Workflow successfully imported.`, 1500);
  onClipboardPermissionsDenied = () => toastNotificationService.show('Clipboard Error', `Clipboard pemission denied.`, 1500);
  onClipboardCopied = (title: string, body: string) => {
    toastNotificationService.show(title || 'Copy to Clipboard', body || 'Activities successfully copied to Clipboard.', 1500);
  }
}
