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
  @State() publishedVersion: number;
  @State() expanded: boolean;
  private i18next: i18n;
  el: HTMLElement;

  @Watch('workflowDefinition')
  async workflowDefinitionChangedHandler(newWorkflow: WorkflowDefinition, oldWorkflow: WorkflowDefinition) {
    if (newWorkflow.version !== oldWorkflow.version || newWorkflow.isPublished !== oldWorkflow.isPublished || newWorkflow.isLatest !== oldWorkflow.isLatest)
      await this.loadPublishedVersion();
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    await this.loadPublishedVersion();
  }

  render() {
    const t = (x, params?) => this.i18next.t(x, params);
    const {workflowDefinition} = this;
    const name = workflowDefinition.name || this.i18next.t("Untitled");
    const {isPublished} = workflowDefinition;

    return (
      <Host>
        <dl class="elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <dt class="elsa-text-gray-500">{t('Name')}</dt>
            <dd class="elsa-text-gray-900">{name}</dd>
          </div>
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <dt class="elsa-text-gray-500">{t('DisplayName')}</dt>
            <dd class="elsa-text-gray-900">{workflowDefinition.displayName || '-'}</dd>
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
      </Host>
    );
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  async loadPublishedVersion() {
    const elsaClient = await this.createClient();
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