import {Component, Event, EventEmitter, h, Host, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {
  ActivityDefinition,
  ActivityDescriptor,
  ActivityModel,
  ConnectionDefinition,
  ConnectionModel,
  EventTypes,
  VersionOptions,
  WorkflowDefinition,
  WorkflowModel,
  WorkflowPersistenceBehavior
} from "../../../../models";
import {eventBus, createElsaClient, SaveWorkflowDefinitionRequest} from "../../../../services";
import state from '../../../../utils/store';
import WorkflowEditorTunnel, {WorkflowEditorState} from '../../../../data/workflow-editor';
import DashboardTunnel from "../../../../data/dashboard";
import {downloadFromBlob} from "../../../../utils/download";
import {ActivityContextMenuState, WorkflowDesignerMode} from "../../../designers/tree/elsa-designer-tree/models";
import {registerClickOutside} from "stencil-click-outside";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";

@Component({
  tag: 'elsa-workflow-definition-editor-screen',
  shadow: false,
})
export class ElsaWorkflowDefinitionEditorScreen {

  @Event() workflowSaved: EventEmitter<WorkflowDefinition>;
  @Prop({attribute: 'workflow-definition-id', reflect: true}) workflowDefinitionId: string;
  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;
  @Prop() culture: string;
  @State() workflowDefinition: WorkflowDefinition;
  @State() workflowModel: WorkflowModel;
  @State() publishing: boolean;
  @State() unPublishing: boolean;
  @State() unPublished: boolean;
  @State() saving: boolean;
  @State() saved: boolean;
  @State() importing: boolean;
  @State() imported: boolean;
  @State() networkError: string;

  @State() activityContextMenuState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
  };

  i18next: i18n;
  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;

  @Method()
  async getServerUrl(): Promise<string> {
    return this.serverUrl;
  }

  @Method()
  async getWorkflowDefinitionId(): Promise<string> {
    return this.workflowDefinition.definitionId;
  }

  @Method()
  async exportWorkflow() {
    const client = createElsaClient(this.serverUrl);
    const workflowDefinition = this.workflowDefinition;
    const versionOptions: VersionOptions = {version: workflowDefinition.version};
    const response = await client.workflowDefinitionsApi.export(workflowDefinition.definitionId, versionOptions);
    downloadFromBlob(response.data, {contentType: 'application/json', fileName: response.fileName});
  }

  @Method()
  async importWorkflow(file: File) {
    const client = createElsaClient(this.serverUrl);

    this.importing = true;
    this.imported = false;
    this.networkError = null;

    try {
      const workflowDefinition = await client.workflowDefinitionsApi.import(this.workflowDefinition.definitionId, file);
      this.workflowDefinition = workflowDefinition;
      this.workflowModel = this.mapWorkflowModel(workflowDefinition);

      this.importing = false;
      this.imported = true;
      setTimeout(() => this.imported = false, 500);
      eventBus.emit(EventTypes.WorkflowImported, this, this.workflowDefinition);
    } catch (e) {
      console.error(e);
      this.importing = false;
      this.imported = false;
      this.networkError = e.message;
      setTimeout(() => this.networkError = null, 10000);
    }
  }

  @Watch('workflowDefinitionId')
  async workflowDefinitionIdChangedHandler(newValue: string) {
    const workflowDefinitionId = newValue;
    let workflowDefinition: WorkflowDefinition = ElsaWorkflowDefinitionEditorScreen.createWorkflowDefinition();
    workflowDefinition.definitionId = workflowDefinitionId;
    const client = createElsaClient(this.serverUrl);

    if (workflowDefinitionId && workflowDefinitionId.length > 0) {
      try {
        workflowDefinition = await client.workflowDefinitionsApi.getByDefinitionAndVersion(workflowDefinitionId, {isLatest: true});
      } catch {
        console.warn(`The specified workflow definition does not exist. Creating a new one.`)
      }
    }

    this.updateWorkflowDefinition(workflowDefinition);
  }

  @Watch("serverUrl")
  async serverUrlChangedHandler(newValue: string) {
    if (newValue && newValue.length > 0) {
      await this.loadActivityDescriptors();
      await this.loadWorkflowStorageDescriptors();
    }
  }

  @Watch("monacoLibPath")
  async monacoLibPathChangedHandler(newValue: string) {
    state.monacoLibPath = newValue;
  }

  @Listen('workflow-changed')
  async workflowChangedHandler(event: CustomEvent<WorkflowModel>) {
    const workflowModel = event.detail;
    await this.saveWorkflowInternal(workflowModel);
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    await this.serverUrlChangedHandler(this.serverUrl);
    await this.workflowDefinitionIdChangedHandler(this.workflowDefinitionId);
    await this.monacoLibPathChangedHandler(this.monacoLibPath);
  }

  async componentDidLoad() {

    if (!this.designer) {
      this.designer = this.el.querySelector("elsa-designer-tree") as HTMLElsaDesignerTreeElement;
      this.designer.model = this.workflowModel;
    }
  }

  connectedCallback() {
    eventBus.on(EventTypes.UpdateWorkflowSettings, this.onUpdateWorkflowSettings);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.UpdateWorkflowSettings, this.onUpdateWorkflowSettings);
  }

  t = (key: string) => this.i18next.t(key);

  async loadActivityDescriptors() {
    const client = createElsaClient(this.serverUrl);
    state.activityDescriptors = await client.activitiesApi.list();
  }

  async loadWorkflowStorageDescriptors() {
    const client = createElsaClient(this.serverUrl);
    state.workflowStorageDescriptors = await client.workflowStorageProvidersApi.list();
  }

  updateWorkflowDefinition(value: WorkflowDefinition) {
    this.workflowDefinition = value;
    this.workflowModel = this.mapWorkflowModel(value);
  }

  async publishWorkflow() {
    this.publishing = true;
    await this.saveWorkflow(true);
    this.publishing = false;
    eventBus.emit(EventTypes.WorkflowPublished, this, this.workflowDefinition);
  }

  async unPublishWorkflow() {
    this.unPublishing = true;
    await this.unpublishWorkflow();
    this.unPublishing = false;
    eventBus.emit(EventTypes.WorkflowRetracted, this, this.workflowDefinition);
  }

  async saveWorkflow(publish?: boolean) {
    await this.saveWorkflowInternal(null, publish);
  }

  async saveWorkflowInternal(workflowModel?: WorkflowModel, publish?: boolean) {
    if (!this.serverUrl || this.serverUrl.length == 0)
      return;

    workflowModel = workflowModel || this.workflowModel;

    const client = createElsaClient(this.serverUrl);
    let workflowDefinition = this.workflowDefinition;

    const request: SaveWorkflowDefinitionRequest = {
      workflowDefinitionId: workflowDefinition.definitionId || this.workflowDefinitionId,
      contextOptions: workflowDefinition.contextOptions,
      deleteCompletedInstances: workflowDefinition.deleteCompletedInstances,
      description: workflowDefinition.description,
      displayName: workflowDefinition.displayName,
      isSingleton: workflowDefinition.isSingleton,
      name: workflowDefinition.name,
      tag: workflowDefinition.tag,
      channel: workflowDefinition.channel,
      persistenceBehavior: workflowDefinition.persistenceBehavior,
      publish: publish || false,
      variables: workflowDefinition.variables,
      activities: workflowModel.activities.map<ActivityDefinition>(x => ({
        activityId: x.activityId,
        type: x.type,
        name: x.name,
        displayName: x.displayName,
        description: x.description,
        persistWorkflow: x.persistWorkflow,
        loadWorkflowContext: x.loadWorkflowContext,
        saveWorkflowContext: x.saveWorkflowContext,
        properties: x.properties,
        propertyStorageProviders: x.propertyStorageProviders
      })),
      connections: workflowModel.connections.map<ConnectionDefinition>(x => ({
        sourceActivityId: x.sourceId,
        targetActivityId: x.targetId,
        outcome: x.outcome
      })),
    };

    this.saving = !publish;
    this.publishing = publish;

    try {
      console.debug("Saving workflow...");
      workflowDefinition = await client.workflowDefinitionsApi.save(request);
      this.workflowDefinition = workflowDefinition;
      this.workflowModel = this.mapWorkflowModel(workflowDefinition);

      this.saving = false;
      this.saved = !publish;
      this.publishing = false;
      setTimeout(() => this.saved = false, 500);
      this.workflowSaved.emit(workflowDefinition);
    } catch (e) {
      console.error(e);
      this.saving = false;
      this.saved = false;
      this.networkError = e.message;
      setTimeout(() => this.networkError = null, 10000);
    }
  }

  async unpublishWorkflow() {
    const client = createElsaClient(this.serverUrl);
    const workflowDefinitionId = this.workflowDefinition.definitionId;
    this.unPublishing = true;

    try {
      this.workflowDefinition = await client.workflowDefinitionsApi.retract(workflowDefinitionId);
      this.unPublishing = false;
      this.unPublished = true
      setTimeout(() => this.unPublished = false, 500);
    } catch (e) {
      console.error(e);
      this.unPublishing = false;
      this.unPublished = false;
      this.networkError = e.message;
      setTimeout(() => this.networkError = null, 2000);
    }
  }

  mapWorkflowModel(workflowDefinition: WorkflowDefinition): WorkflowModel {
    return {
      activities: workflowDefinition.activities.map(this.mapActivityModel),
      connections: workflowDefinition.connections.map(this.mapConnectionModel),
      persistenceBehavior: workflowDefinition.persistenceBehavior,

    };
  }

  mapActivityModel(source: ActivityDefinition): ActivityModel {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    const activityDescriptor = activityDescriptors.find(x => x.type == source.type);
    const outcomes = !!activityDescriptor ? activityDescriptor.outcomes : ['Done'];

    return {
      activityId: source.activityId,
      description: source.description,
      displayName: source.displayName,
      name: source.name,
      type: source.type,
      properties: source.properties,
      outcomes: [...outcomes],
      persistWorkflow: source.persistWorkflow,
      saveWorkflowContext: source.saveWorkflowContext,
      loadWorkflowContext: source.loadWorkflowContext,
      propertyStorageProviders: source.propertyStorageProviders
    }
  }

  mapConnectionModel(source: ConnectionDefinition): ConnectionModel {
    return {
      sourceId: source.sourceActivityId,
      targetId: source.targetActivityId,
      outcome: source.outcome
    }
  }

  handleContextMenuChange(state: ActivityContextMenuState) {
    this.activityContextMenuState = state;
  }

  onShowWorkflowSettingsClick() {
    eventBus.emit(EventTypes.ShowWorkflowSettings);
  }

  async onPublishClicked() {
    await this.publishWorkflow();
  }

  async onUnPublishClicked() {
    await this.unPublishWorkflow();
  }

  async onExportClicked() {
    await this.exportWorkflow();
  }

  async onImportClicked(file: File) {
    await this.importWorkflow(file);
  }

  async onDeleteActivityClick(e: Event) {
    e.preventDefault();
    await this.designer.removeActivity(this.activityContextMenuState.activity);
    this.handleContextMenuChange({x: 0, y: 0, shown: false, activity: null});
  }

  async onEditActivityClick(e: Event) {
    e.preventDefault();
    await this.designer.showActivityEditor(this.activityContextMenuState.activity, true);
    this.handleContextMenuChange({x: 0, y: 0, shown: false, activity: null});
  }

  onActivityContextMenuButtonClicked(e: CustomEvent<ActivityContextMenuState>) {
    this.activityContextMenuState = e.detail;
  }

  private onUpdateWorkflowSettings = async (workflowDefinition: WorkflowDefinition) => {
    this.updateWorkflowDefinition(workflowDefinition);
    await this.saveWorkflowInternal(this.workflowModel);
  }

  render() {
    const tunnelState: WorkflowEditorState = {
      serverUrl: this.serverUrl,
      workflowDefinitionId: this.workflowDefinition.definitionId
    };

    return (
      <Host class="elsa-flex elsa-flex-col elsa-w-full" ref={el => this.el = el}>
        <WorkflowEditorTunnel.Provider state={tunnelState}>
          {this.renderCanvas()}
          {this.renderActivityPicker()}
          {this.renderActivityEditor()}
        </WorkflowEditorTunnel.Provider>
      </Host>
    );
  }

  renderCanvas() {

    const activityContextMenuButton = (activity: ActivityModel) =>
      `<div class="context-menu-wrapper elsa-flex-shrink-0">
            <button aria-haspopup="true"
                    class="elsa-w-8 elsa-h-8 elsa-inline-flex elsa-items-center elsa-justify-center elsa-text-gray-400 elsa-rounded-full elsa-bg-transparent hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-text-gray-500 focus:elsa-bg-gray-100 elsa-transition elsa-ease-in-out elsa-duration-150">
              <svg class="elsa-h-6 elsa-w-6 elsa-text-gray-400" width="24" height="24" viewBox="0 0 24 24" stroke-width="2"
                   stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                <path stroke="none" d="M0 0h24v24H0z"/>
                <circle cx="5" cy="12" r="1"/>
                <circle cx="12" cy="12" r="1"/>
                <circle cx="19" cy="12" r="1"/>
              </svg>
            </button>
          </div>`;

    return (
      <div class="elsa-flex-1 elsa-flex elsa-relative">
        <elsa-designer-tree model={this.workflowModel}
                            mode={WorkflowDesignerMode.Edit}
                            activityContextMenuButton={activityContextMenuButton}
                            onActivityContextMenuButtonClicked={e => this.onActivityContextMenuButtonClicked(e)}
                            activityContextMenu={this.activityContextMenuState}
                            enableMultipleConnectionsFromSingleSource={false}
                            class="elsa-flex-1"
                            ref={el => this.designer = el}/>
        {this.renderPropertiesPanel()}
        {this.renderWorkflowSettingsButton()}
        {this.renderActivityContextMenu()}
        <elsa-workflow-settings-modal workflowDefinition={this.workflowDefinition}/>
        <elsa-workflow-definition-editor-notifications/>
        <div class="elsa-fixed elsa-bottom-10 elsa-right-12">
          <div class="elsa-flex elsa-items-center elsa-space-x-4">
            {this.renderSavingIndicator()}
            {this.renderNetworkError()}
            {this.renderPublishButton()}
          </div>
        </div>
      </div>
    );
  }

  renderActivityContextMenu() {
    const t = this.t;

    return <div
      data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
      data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
      data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
      data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
      class={`${this.activityContextMenuState.shown ? '' : 'hidden'} context-menu elsa-z-10 elsa-mx-3 elsa-w-48 elsa-mt-1 elsa-rounded-md elsa-shadow-lg elsa-absolute`}
      style={{left: `${this.activityContextMenuState.x}px`, top: `${this.activityContextMenuState.y - 64}px`}}
      ref={el =>
        registerClickOutside(this, el, () => {
          this.handleContextMenuChange({x: 0, y: 0, shown: false, activity: null});
        })
      }
    >
      <div class="elsa-rounded-md elsa-bg-white elsa-shadow-xs" role="menu" aria-orientation="vertical" aria-labelledby="pinned-project-options-menu-0">
        <div class="elsa-py-1">
          <a
            onClick={e => this.onEditActivityClick(e)}
            href="#"
            class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-leading-5 elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900 focus:elsa-outline-none focus:elsa-bg-gray-100 focus:elsa-text-gray-900"
            role="menuitem">
            {t('ActivityContextMenu.Edit')}
          </a>
        </div>
        <div class="elsa-border-t elsa-border-gray-100"/>
        <div class="elsa-py-1">
          <a
            onClick={e => this.onDeleteActivityClick(e)}
            href="#"
            class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-leading-5 elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900 focus:elsa-outline-none focus:elsa-bg-gray-100 focus:elsa-text-gray-900"
            role="menuitem">
            {t('ActivityContextMenu.Delete')}
          </a>
        </div>
      </div>
    </div>
  }

  renderActivityPicker() {
    return <elsa-activity-picker-modal/>;
  }

  renderActivityEditor() {
    return <elsa-activity-editor-modal culture={this.culture}/>;
  }

  renderWorkflowSettingsButton() {
    return (
      <button onClick={() => this.onShowWorkflowSettingsClick()} type="button"
              class="workflow-settings-button elsa-fixed elsa-top-20 elsa-right-12 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke="currentColor" fill="none" class="elsa-h-8 elsa-w-8">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
        </svg>
      </button>
    );
  }

  renderWorkflowSettingsModal() {
    return;
  }

  renderSavingIndicator() {

    if (this.publishing)
      return undefined;

    const t = this.t;
    const message =
      this.unPublishing ? t('Unpublishing...') : this.unPublished ? t('Unpublished')
        : this.saving ? 'Saving...' : this.saved ? 'Saved'
          : this.importing ? 'Importing...' : this.imported ? 'Imported'
            : null;

    if (!message)
      return undefined;

    return (
      <div>
        <span class="elsa-text-gray-400 elsa-text-sm">{message}</span>
      </div>
    );
  }

  renderNetworkError() {
    if (!this.networkError)
      return undefined;

    return (
      <div>
        <span class="elsa-text-rose-400 elsa-text-sm">An error occurred: {this.networkError}</span>
      </div>);
  }

  renderPublishButton() {
    return <elsa-workflow-publish-button
      publishing={this.publishing}
      workflowDefinition={this.workflowDefinition}
      onPublishClicked={() => this.onPublishClicked()}
      onUnPublishClicked={() => this.onUnPublishClicked()}
      onExportClicked={() => this.onExportClicked()}
      onImportClicked={e => this.onImportClicked(e.detail)}
      culture={this.culture}
    />;
  }

  private static createWorkflowDefinition(): WorkflowDefinition {
    return {
      definitionId: null,
      version: 1,
      activities: [],
      connections: [],
      persistenceBehavior: WorkflowPersistenceBehavior.WorkflowBurst,
    };
  }

  private renderPropertiesPanel() {
    return (
      <elsa-workflow-properties-panel
        workflowDefinition={this.workflowDefinition}
        expandButtonPosition={2}
      />);
  }
}
DashboardTunnel.injectProps(ElsaWorkflowDefinitionEditorScreen, ['serverUrl', 'culture', 'monacoLibPath']);
