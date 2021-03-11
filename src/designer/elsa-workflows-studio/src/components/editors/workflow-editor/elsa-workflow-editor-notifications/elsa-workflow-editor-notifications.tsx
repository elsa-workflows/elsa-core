import {Component, Host, h} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import {EventTypes, WorkflowDefinition} from "../../../../models";

@Component({
  tag: 'elsa-workflow-editor-notifications',
  styleUrl: 'elsa-workflow-editor-notifications.css',
  shadow: false,
})
export class ElsaWorkflowEditorNotifications {

  toastNotificationElement: HTMLElsaToastNotificationElement;

  componentDidLoad() {
    eventBus.on(EventTypes.WorkflowPublished, async (workflowDefinition: WorkflowDefinition) => {
      await this.toastNotificationElement.show({autoCloseIn: 1500, title: 'Workflow Published', message: `Workflow successfully published at version ${workflowDefinition.version}.`});
    });

    eventBus.on(EventTypes.WorkflowRetracted, async (workflowDefinition: WorkflowDefinition) => {
      await this.toastNotificationElement.show({autoCloseIn: 1500, title: 'Workflow Unpublished', message: `Workflow successfully retracted.`});
    });

    eventBus.on(EventTypes.WorkflowImported, async (workflowDefinition: WorkflowDefinition) => {
      await this.toastNotificationElement.show({autoCloseIn: 1500, title: 'Workflow Imported', message: `Workflow successfully imported.`});
    });
  }

  render() {

    return (
      <Host>
        <elsa-toast-notification ref={el => this.toastNotificationElement = el}/>
      </Host>
    );
  }
}
