import {Component, Host, h} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import {EventTypes, WorkflowDefinition} from "../../../../models";

@Component({
  tag: 'elsa-workflow-editor-notifications',
  styleUrl: 'elsa-workflow-editor-notifications.css',
  shadow: false,
})
export class ElsaWorkflowEditorNotifications {

  publishedNotification: HTMLElsaToastNotificationElement;

  componentDidLoad() {
    eventBus.on(EventTypes.WorkflowPublished, async (workflowDefinition: WorkflowDefinition) => {
      await this.publishedNotification.show({autoCloseIn: 1500, title: 'Workflow Published', message: `Workflow successfully published at version ${workflowDefinition.version}.`});
    });
  }

  render() {

    return (
      <Host>
        <elsa-toast-notification ref={el => this.publishedNotification = el}/>
      </Host>
    );
  }
}
