import {Component, Host, h} from '@stencil/core';
import {eventBus} from '../../../../../../services/event-bus';
import {EventTypes, WebhookDefinition} from "../../../../models";

@Component({
  tag: 'elsa-webhook-definition-editor-notifications',
  shadow: false,
})
export class ElsaWebhookEditorNotifications {

  toastNotificationElement: HTMLElsaToastNotificationElement;

  connectedCallback() {
    eventBus.on(EventTypes.WebhookSaved, this.onWebhookSaved);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WebhookSaved, this.onWebhookSaved);
  }

  onWebhookSaved = async (webhookDefinition: WebhookDefinition) => await this.toastNotificationElement.show({
    autoCloseIn: 1500,
    title: 'Webhook Saved',
    message: `Webhook successfully saved with name ${webhookDefinition.name}.`
  });

  render() {

    return (
      <Host class="elsa-block">
        <elsa-toast-notification ref={el => this.toastNotificationElement = el}/>
      </Host>
    );
  }
}
