import {Component, h} from '@stencil/core';
import {eventBus} from "../../../services";
import {EventTypes, ConfigureFeatureContext, FeatureMenuItem} from "../../../models";
import {IntlMessage} from '../../../components/i18n/intl-message';

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

    context.menuItems.push({url: "webhook-definitions", label: "WebhookDefinitions", component: null, exact: false})
    context.routes.push({url: "webhook-definitions", label: null, component: "elsa-studio-webhook-definitions-list", exact: true},
                        {url: "webhook-definitions/:id", label: null, component: "elsa-studio-webhook-definitions-edit", exact: false});
  }
}
