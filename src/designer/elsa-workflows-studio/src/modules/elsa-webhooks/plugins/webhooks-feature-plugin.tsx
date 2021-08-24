import {Component, h} from '@stencil/core';
import {eventBus} from "../../../services";
import {EventTypes, ConfigureFeatureContext} from "../../../models";

@Component({
    tag: 'elsa-webhooks-feature-plugin',
    shadow: false,
})
export class ElsaWebhooksFeaturePlugin {

  connectedCallback() {
    eventBus.on(EventTypes.ConfigureFeature, this.onWebhooksEnabled);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ConfigureFeature, this.onWebhooksEnabled);
  }

  onWebhooksEnabled(context: ConfigureFeatureContext) {
    if (context.featureName != "webhooks")
      return;

    if (context.component != "ElsaStudioDashboard")
      return;

    const menuItems: any[] = [["webhook-definitions", "WebhookDefinitions"]];
    const routes: any[] = [["webhook-definitions", "elsa-studio-webhook-definitions-list", true],
                         ["webhook-definitions/:id", "elsa-studio-webhook-definitions-edit", false]];

    context.data = {menuItems, routes};
  }
}