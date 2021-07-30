import {Component, h, Prop, State} from '@stencil/core';
import 'i18next-wc';
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {GetIntlMessage} from "../../../i18n/intl-message";
import {i18n} from "i18next";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-studio-webhook-definitions-list',
  shadow: false,
})
export class ElsaStudioWebhookDefinitionsList {
  @Prop() culture: string;
  @Prop() basePath: string;
  private i18next: i18n;
  
  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  render() {
    const basePath = this.basePath;
    const IntlMessage = GetIntlMessage(this.i18next);
    
    return (
      <div>
        <div class="elsa-border-b elsa-border-gray-200 elsa-px-4 elsa-py-4 sm:elsa-flex sm:elsa-items-center sm:elsa-justify-between sm:elsa-px-6 lg:elsa-px-8 elsa-bg-white">
          <div class="elsa-flex-1 elsa-min-w-0">
            <h1 class="elsa-text-lg elsa-font-medium elsa-leading-6 elsa-text-gray-900 sm:elsa-truncate">
              <IntlMessage label="Title"/>
            </h1>
          </div>
          <div class="elsa-mt-4 elsa-flex sm:elsa-mt-0 sm:elsa-ml-4">
            <stencil-route-link url={`${basePath}/webhook-definitions/new`}
                                class="elsa-order-0 elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-transparent elsa-shadow-sm elsa-text-sm elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-blue-600 hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-order-1 sm:elsa-ml-3">
              <IntlMessage label="CreateButton"/>
            </stencil-route-link>
          </div>
        </div>

        <elsa-webhook-definitions-list-screen />
      </div>
    );
  }
}
Tunnel.injectProps(ElsaStudioWebhookDefinitionsList, ['culture', 'basePath']);