import {Component, Prop, h, State, Watch, Host} from '@stencil/core';
import {enter, leave} from "el-transition"
import {WorkflowDefinition, WorkflowDefinitionSummary} from "../../../../models";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {createElsaClient} from "../../../../services";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-workflow-properties-panel',
  shadow: false
})
export class ElsaWorkflowPropertiesPanel {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() culture: string;
  @Prop() serverUrl: string;
  @Prop() expandButtonPosition = 1;
  @State() publishedVersion: number;
  @State() expanded: boolean;
  private i18next: i18n;
  el: HTMLElement;

  @Watch('workflowDefinition')
  async workflowDefinitionChangedHandler(newWorkflow: WorkflowDefinition, oldWorkflow: WorkflowDefinition) {
    if (newWorkflow.isPublished !== oldWorkflow.isPublished && newWorkflow.isPublished) {
      await this.loadPublishedVersion();
    }
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    await this.loadPublishedVersion();
  }

  render() {
    const t = (x, params?) => this.i18next.t(x, params);
    const {workflowDefinition, expanded, expandButtonPosition} = this;
    const name = workflowDefinition.name || this.i18next.t("Untitled");
    const {isPublished} = workflowDefinition;
    const expandPositionClass = expandButtonPosition == 1 ? "elsa-right-12" : "elsa-right-28";

    return (
      <Host>
        <button type="button"
                onClick={this.toggle}
                class={`${expanded ? "hidden" : expandPositionClass} workflow-settings-button elsa-fixed elsa-top-20 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 elsa-z-10`}>
          <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-8 elsa-w-8" fill="none" viewBox="0 0 24 24"
               stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 19l-7-7 7-7m8 14l-7-7 7-7"/>
          </svg>
        </button>
        <section
          class={`${this.expanded ? '' : 'hidden'} elsa-fixed elsa-top-4 elsa-right-0 elsa-bottom-0 elsa-overflow-hidden`}
          aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
          <div class="elsa-absolute elsa-inset-0 elsa-overflow-hidden">
            <div class="elsa-absolute elsa-inset-0" aria-hidden="true"/>
            <div
              class="elsa-fixed elsa-inset-y-0 elsa-top-16 elsa-right-0 max-elsa-w-full elsa-flex">
              <div
                ref={el => this.el = el}
                data-transition-enter="elsa-transform elsa-transition elsa-ease-in-out elsa-duration-300 sm:elsa-duration-700"
                data-transition-enter-start="elsa-translate-x-full"
                data-transition-enter-end="elsa-translate-x-0"
                data-transition-leave="elsa-transform elsa-transition elsa-ease-in-out elsa-duration-300 sm:elsa-duration-700"
                data-transition-leave-start="elsa-translate-x-0"
                data-transition-leave-end="elsa-translate-x-full"
                class="elsa-w-screen elsa-max-w-lg elsa-h-full">
                <button type="button"
                        onClick={this.toggle}
                        class="workflow-settings-button elsa-absolute elsa-top-4 elsa-left-2 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 elsa-z-10">
                  <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-8 elsa-w-8" fill="none" viewBox="0 0 24 24"
                       stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 5l7 7-7 7M5 5l7 7-7 7"/>
                  </svg>
                </button>
                <div
                  class="elsa-h-full elsa-flex elsa-flex-col elsa-py-6 elsa-bg-white elsa-shadow-xl elsa-overflow-y-scroll elsa-bg-white">
                  <div class="elsa-h-full">
                    <div class="elsa-mt-16 elsa-p-6">
                      <div class="elsa-font-medium elsa-leading-8 elsa-overflow-hidden">
                        <p
                          class="elsa-overflow-ellipsis">{t('Properties', {name: workflowDefinition.displayName || name})}</p>
                      </div>
                      <div>
                        <dl
                          class="elsa-mt-2 elsa-border-t elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
                          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                            <dt class="elsa-text-gray-500">{t('Name')}</dt>
                            <dd class="elsa-text-gray-900">{name}</dd>
                          </div>
                          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                            <dt class="elsa-text-gray-500">{t('Id')}</dt>
                            <dd class="elsa-text-gray-900 elsa-break-all">{workflowDefinition.definitionId || '-'}</dd>
                          </div>
                          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                            <dt class="elsa-text-gray-500">{t('Version')}</dt>
                            <dd class="elsa-text-gray-900">{workflowDefinition.version}</dd>
                          </div>
                          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                            <dt class="elsa-text-gray-500">{t('PublishedVersion')}</dt>
                            <dd class="elsa-text-gray-900">{this.publishedVersion || '-'}</dd>
                          </div>
                          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                            <dt class="elsa-text-gray-500">{t('Status')}</dt>
                            <dd
                              class={`${isPublished ? 'elsa-text-green-600' : 'elsa-text-yellow-700'}`}>{isPublished ? t('Published') : t('Draft')}</dd>
                          </div>
                        </dl>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>
      </Host>
    );
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  async loadPublishedVersion() {
    const elsaClient = this.createClient();
    const {workflowDefinition} = this;

    const publishedWorkflowDefinitions = await elsaClient.workflowDefinitionsApi.getMany([workflowDefinition.definitionId], {isPublished: true});
    const publishedDefinition: WorkflowDefinitionSummary = workflowDefinition.isPublished ? workflowDefinition : publishedWorkflowDefinitions.find(x => x.definitionId == workflowDefinition.definitionId);
    if (publishedDefinition) {
      this.publishedVersion = publishedDefinition.version;
    }
  }

  toggle = () => {
    if (this.expanded) {
      leave(this.el).then(() => this.expanded = false);
    } else {
      this.expanded = true;
      enter(this.el);
    }
  }
}

Tunnel.injectProps(ElsaWorkflowPropertiesPanel, ['serverUrl', 'culture']);
