import {Component} from '@stencil/core';
import {eventBus} from "../../../services";
import {EventTypes, WebhooksEnabledContext} from "../../../models";

@Component({
    tag: 'elsa-webhooks-plugin',
    shadow: false,
})
export class ElsaWebhooksPlugin {

  connectedCallback() {
    eventBus.on(EventTypes.WebhooksEnabled, this.onWebhooksEnabled);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WebhooksEnabled, this.onWebhooksEnabled);
  }

  onWebhooksEnabled(context: WebhooksEnabledContext) {
    context.isEnabled = true;
  }
}
