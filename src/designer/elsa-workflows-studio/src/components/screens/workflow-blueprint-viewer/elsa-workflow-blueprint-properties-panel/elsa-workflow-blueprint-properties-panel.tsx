import {Component, Prop, h, State, Watch, Host} from '@stencil/core';
import {WorkflowBlueprint, WorkflowDefinitionSummary} from "../../../../models";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {createElsaClient} from "../../../../services";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-workflow-blueprint-properties-panel',
  shadow: false
})
export class ElsaWorkflowBlueprintPropertiesPanel {

  @Prop() workflowId: string;
  @Prop() culture: string;
  @Prop() serverUrl: string;
  @State() workflowBlueprint: WorkflowBlueprint;
  @State() publishedVersion: number;
  private i18next: i18n;

  @Watch('workflowId')
  async workflowIdChangedHandler(newWorkflowId: string) {
    await this.loadWorkflowBlueprint(newWorkflowId);
    await this.loadPublishedVersion();
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    await this.loadWorkflowBlueprint();
    await this.loadPublishedVersion();
  }

  render() {
    const t = (x, params?) => this.i18next.t(x, params);
    const {workflowBlueprint} = this;
    const name = workflowBlueprint.name || this.i18next.t("Untitled");
    const {isPublished} = workflowBlueprint;

    return (
      <Host>
        <dl class="elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <dt class="elsa-text-gray-500">{t('Name')}</dt>
            <dd class="elsa-text-gray-900">{name}</dd>
          </div>
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <dt class="elsa-text-gray-500">{t('Id')}</dt>
            <dd class="elsa-text-gray-900 elsa-break-all">{workflowBlueprint.id || '-'}</dd>
          </div>
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <dt class="elsa-text-gray-500">{t('Version')}</dt>
            <dd class="elsa-text-gray-900">{workflowBlueprint.version}</dd>
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
    )
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  async loadPublishedVersion() {
    const elsaClient = await this.createClient();
    const {workflowBlueprint} = this;

    if (workflowBlueprint.isPublished) {
      const publishedWorkflowDefinitions = await elsaClient.workflowDefinitionsApi.getMany([workflowBlueprint.id], {isPublished: true});
      const publishedDefinition: WorkflowDefinitionSummary = workflowBlueprint.isPublished ? workflowBlueprint : publishedWorkflowDefinitions.find(x => x.definitionId == workflowBlueprint.id);

      if (publishedDefinition) {
        this.publishedVersion = publishedDefinition.version;
      }
    } else {
      this.publishedVersion = 0;
    }
  }

  async loadWorkflowBlueprint(workflowId = this.workflowId) {
    const elsaClient = await this.createClient();

    this.workflowBlueprint = await elsaClient.workflowRegistryApi.get(workflowId, {isLatest: true});
  }
}

Tunnel.injectProps(ElsaWorkflowBlueprintPropertiesPanel, ['serverUrl', 'culture']);
