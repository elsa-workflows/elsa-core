import {Component, h, Prop, State} from '@stencil/core';
import 'i18next-wc';
import {i18n} from "i18next";
import { resources } from '../../../components/controls/elsa-pager/localizations';
import { loadTranslations } from '../../../components/i18n/i18n-loader';
import Tunnel from "../../../data/dashboard";
import { eventBus } from '../../../services';
import { SecretEventTypes } from "../models/secret.events";
import state from '../../../utils/store';

@Component({
  tag: 'elsa-credential-manager-items-list',
  shadow: false,
})
export class CredentialManagerItemsList {
  @Prop({ attribute: 'monaco-lib-path' }) monacoLibPath: string;
  @Prop() culture: string;
  @Prop() basePath: string;
  @Prop() serverUrl: string;
  private i18next: i18n;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  async onNewClick() {
    await eventBus.emit(SecretEventTypes.ShowSecretsPicker);
  }

  renderListScreen() {
    return <elsa-credential-manager-list-screen />;
  }

  renderSecretPickerModal() {
    return <elsa-secrets-picker-modal />
  }

  renderSecretPickerEditor() {
    const monacoLibPath = this.monacoLibPath ?? state.monacoLibPath;
    return <elsa-secret-editor-modal culture={this.culture} monaco-lib-path={monacoLibPath} serverUrl={this.serverUrl}/>;
  }

  render() {
    return (
      <div>
        <div class="elsa-border-b elsa-border-gray-200 elsa-px-4 elsa-py-4 sm:elsa-flex sm:elsa-items-center sm:elsa-justify-between sm:elsa-px-6 lg:elsa-px-8 elsa-bg-white">
          <div class="elsa-flex-1 elsa-min-w-0">
            <h1 class="elsa-text-lg elsa-font-medium elsa-leading-6 elsa-text-gray-900 sm:elsa-truncate">
              {/* <IntlMessage label="Title"/> */}
            </h1>
          </div>
          <button type="button"
                      onClick={() => this.onNewClick()}
                      class="elsa-mt-3 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-white elsa-text-base elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-mt-0 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                Add New
              </button>
        </div>
        {this.renderListScreen()}
        {this.renderSecretPickerModal()}
        {this.renderSecretPickerEditor()}
      </div>
    );
  }
}
Tunnel.injectProps(CredentialManagerItemsList, ['culture', 'basePath', 'serverUrl', 'monacoLibPath']);
