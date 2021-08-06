import {Component, h, Prop, State} from '@stencil/core';
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";

@Component({
  tag: 'elsa-studio-workflow-instances-list',
  shadow: false,
})
export class ElsaStudioWorkflowInstancesList {
  @Prop() culture: string;
  private i18next: i18n;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  render() {
    const t = x => this.i18next.t(x);
    
    return (
      <div>
        <div class="elsa-border-b elsa-border-gray-200 elsa-px-4 elsa-py-4 sm:elsa-flex sm:elsa-items-center sm:elsa-justify-between sm:elsa-px-6 lg:elsa-px-8 elsa-bg-white">
          <div class="elsa-flex-1 elsa-min-w-0">
            <h1 class="elsa-text-lg elsa-font-medium elsa-leading-6 elsa-text-gray-900 sm:elsa-truncate">
              {t('Title')}
            </h1>
          </div>
        </div>

        <elsa-workflow-instance-list-screen />
      </div>
    );
  }
}
