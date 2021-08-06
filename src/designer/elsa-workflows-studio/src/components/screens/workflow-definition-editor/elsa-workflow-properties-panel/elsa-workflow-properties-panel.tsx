import {Component, Prop, h, State} from '@stencil/core';
import {PagedList, VersionOptions, WorkflowDefinition, WorkflowDefinitionSummary} from "../../../../models";
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
  @Prop({attribute: 'server-url'}) serverUrl: string;
  @State() publishedVersion: number;
  private i18next: i18n;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    await this.loadPublishedVersion();
  }

  render() {
    const t = (x, params?) => this.i18next.t(x, params);
    const {workflowDefinition} = this;
    const name = workflowDefinition.name || this.i18next.t('Untitled');
    const {isPublished} = workflowDefinition;

    console.log('workflowDefinition2', this.workflowDefinition);
    return (
      <div class="h-full">
        <div class="elsa-mt-16 elsa-p-6">
          <div class="elsa-font-medium elsa-leading-8 elsa-overflow-hidden">
            <p class="elsa-overflow-ellipsis">{t('Properties', {name: workflowDefinition.displayName || name})}</p>
          </div>
          <div>
            <dl class="elsa-mt-2 elsa-border-t elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
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
                <dd class={`${isPublished ? 'elsa-text-green-600' : 'elsa-text-yellow-700'}`}>{isPublished ? t('Published') : t('Draft')}</dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
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
    this.publishedVersion = publishedDefinition.version;

    console.log('this.publishedVersion', this.publishedVersion);
  }
}

Tunnel.injectProps(ElsaWorkflowPropertiesPanel, ['serverUrl', 'culture']);
