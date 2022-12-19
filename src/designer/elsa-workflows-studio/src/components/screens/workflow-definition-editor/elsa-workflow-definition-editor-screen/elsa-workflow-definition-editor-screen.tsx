import { Component, Event, EventEmitter, h, Host, Listen, Method, Prop, State, Watch } from '@stencil/core';
import { injectHistory, RouterHistory } from '@stencil/router';
import {
  ActivityDefinition,
  ActivityDescriptor,
  ActivityModel,
  ComponentCustomButtonClickContext,
  ConfigureComponentCustomButtonContext,
  ConnectionDefinition,
  ConnectionModel,
  EventTypes,
  VersionOptions,
  WorkflowDefinition,
  WorkflowDefinitionVersion,
  WorkflowInstance,
  WorkflowModel,
  WorkflowPersistenceBehavior,
  WorkflowTestActivityMessage,
  WorkflowTestActivityMessageStatus,
} from '../../../../models';
import { ActivityStats, createElsaClient, eventBus, featuresDataManager, monacoEditorDialogService, SaveWorkflowDefinitionRequest } from '../../../../services';
import state from '../../../../utils/store';
import WorkflowEditorTunnel, { WorkflowEditorState } from '../../../../data/workflow-editor';
import DashboardTunnel from '../../../../data/dashboard';
import { downloadFromBlob } from '../../../../utils/download';
import { ActivityContextMenuState, LayoutDirection, WorkflowDesignerMode } from '../../../designers/tree/elsa-designer-tree/models';
import { i18n } from 'i18next';
import { loadTranslations } from '../../../i18n/i18n-loader';
import { resources } from './localizations';
import * as collection from 'lodash/collection';
import { tr } from 'cronstrue/dist/i18n/locales/tr';

@Component({
  tag: 'elsa-workflow-definition-editor-screen',
  styleUrl: 'elsa-workflow-definition-editor-screen.css',
  shadow: false,
})
export class ElsaWorkflowDefinitionEditorScreen {
  @Event() workflowSaved: EventEmitter<WorkflowDefinition>;
  @Prop({ attribute: 'workflow-definition-id', reflect: true }) workflowDefinitionId: string;
  @Prop({ attribute: 'server-url', reflect: true }) serverUrl: string;
  @Prop({ attribute: 'monaco-lib-path', reflect: true }) monacoLibPath: string;
  @Prop() features: string;
  @Prop() culture: string;
  @Prop() basePath: string;
  @Prop() serverFeatures: Array<string> = [];
  @Prop() history: RouterHistory;
  @State() workflowDefinition: WorkflowDefinition;
  @State() workflowModel: WorkflowModel;
  @State() publishing: boolean;
  @State() unPublishing: boolean;
  @State() unPublished: boolean;
  @State() saving: boolean;
  @State() saved: boolean;
  @State() importing: boolean;
  @State() imported: boolean;
  @State() reverting: boolean;
  @State() reverted: boolean;
  @State() networkError: string;
  @State() selectedActivityId?: string;
  @State() workflowDesignerMode: WorkflowDesignerMode;
  @State() workflowTestActivityMessages: Array<WorkflowTestActivityMessage> = [];
  @State() workflowInstance?: WorkflowInstance;
  @State() workflowInstanceId?: string;
  @State() activityStats?: ActivityStats;
  @State() layoutDirection: LayoutDirection = LayoutDirection.TopBottom;//???

  @State() activityContextMenuState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
    selectedActivities: {},
  };

  // @State() connectionContextMenuState: ActivityContextMenuState = {
  //   shown: false,
  //   x: 0,
  //   y: 0,
  //   activity: null,
  // };

  @State() activityContextMenuTestState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
  };

  i18next: i18n;
  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;
  configureComponentCustomButtonContext: ConfigureComponentCustomButtonContext = null;
  helpDialog: HTMLElsaModalDialogElement;
  activityContextMenu: HTMLDivElement;
  componentCustomButton: HTMLDivElement;
  confirmDialog: HTMLElsaConfirmDialogElement;

  //connectionContextMenu: HTMLDivElement;

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
    const client = await createElsaClient(this.serverUrl);
    const workflowDefinition = this.workflowDefinition;
    const versionOptions: VersionOptions = { version: workflowDefinition.version };
    const response = await client.workflowDefinitionsApi.export(workflowDefinition.definitionId, versionOptions);
    downloadFromBlob(response.data, { contentType: 'application/json', fileName: response.fileName });
  }

  @Method()
  async importWorkflow(file: File) {
    const client = await createElsaClient(this.serverUrl);

    this.importing = true;
    this.imported = false;
    this.networkError = null;

    try {
      const workflowDefinition = await client.workflowDefinitionsApi.import(this.workflowDefinition.definitionId, file);
      this.workflowDefinition = workflowDefinition;
      this.workflowModel = this.mapWorkflowModel(workflowDefinition);
      this.updateUrl(workflowDefinition.definitionId);

      this.importing = false;
      this.imported = true;
      setTimeout(() => (this.imported = false), 500);
      await eventBus.emit(EventTypes.WorkflowImported, this, this.workflowModel);
    } catch (e) {
      console.error(e);
      this.importing = false;
      this.imported = false;
      this.networkError = e.message;
      setTimeout(() => (this.networkError = null), 10000);
    }
  }

  @Watch('workflowDefinitionId')
  async workflowDefinitionIdChangedHandler(newValue: string) {
    const workflowDefinitionId = newValue;
    let workflowDefinition: WorkflowDefinition = ElsaWorkflowDefinitionEditorScreen.createWorkflowDefinition();
    workflowDefinition.definitionId = workflowDefinitionId;
    const client = await createElsaClient(this.serverUrl);

    if (workflowDefinitionId && workflowDefinitionId.length > 0) {
      try {
        workflowDefinition = await client.workflowDefinitionsApi.getByDefinitionAndVersion(workflowDefinitionId, { isLatest: true });
      } catch {
        console.warn(`The specified workflow definition does not exist. Creating a new one.`);
      }
    }

    this.updateWorkflowDefinition(workflowDefinition);
  }

  @Watch('serverUrl')
  async serverUrlChangedHandler(newValue: string) {
    if (newValue && newValue.length > 0) {
      await this.loadActivityDescriptors();
      await this.loadWorkflowStorageDescriptors();
    }
  }

  @Watch('monacoLibPath')
  async monacoLibPathChangedHandler(newValue: string) {
    state.monacoLibPath = newValue;
  }

  @Listen('workflow-changed')
  async workflowChangedHandler(event: CustomEvent<WorkflowModel>) {
    const workflowModel = event.detail;
    await this.saveWorkflowInternal(workflowModel);
  }

  @Listen('click', { target: 'window' })
  onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.componentCustomButton.contains(target)) this.handleContextMenuTestChange(0, 0, false, null);

    if (!this.activityContextMenu.contains(target)) this.handleContextMenuChange({ x: 0, y: 0, shown: false, activity: null, selectedActivities: {} });

    // if (!this.connectionContextMenu.contains(target))
    //   this.handleConnectionContextMenuChange({x: 0, y: 0, shown: false, activity: null});
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    this.workflowDesignerMode = WorkflowDesignerMode.Edit;

    const layoutFeature = featuresDataManager.getFeatureConfig(featuresDataManager.supportedFeatures.workflowLayout);
    if (layoutFeature && layoutFeature.enabled) {
      this.layoutDirection = layoutFeature.value as LayoutDirection;
    }
    await this.serverUrlChangedHandler(this.serverUrl);
    await this.workflowDefinitionIdChangedHandler(this.workflowDefinitionId);
    await this.monacoLibPathChangedHandler(this.monacoLibPath);
  }

  async componentDidLoad() {
    if (!this.designer) {
      if (state.useX6Graphs) {
        this.designer = this.el.querySelector("x6-designer") as HTMLX6DesignerElement;
      } else {
        this.designer = this.el.querySelector('elsa-designer-tree') as HTMLElsaDesignerTreeElement;
      }
      this.designer.model = this.workflowModel;
    }
  }

  componentDidRender() {
    if (this.el && this.componentCustomButton) {
      let modalX = this.activityContextMenuTestState.x + 64;
      let modalY = this.activityContextMenuTestState.y - 256;

      // Fit the modal to the canvas bounds
      const canvasBounds = this.el?.getBoundingClientRect();
      const modalBounds = this.componentCustomButton.getBoundingClientRect();
      const modalWidth = modalBounds?.width;
      const modalHeight = modalBounds?.height;
      modalX = Math.min(canvasBounds.width, modalX + modalWidth + 32) - modalWidth - 32;
      modalY = Math.min(canvasBounds.height, modalY + modalHeight) - modalHeight - 32;
      modalY = Math.max(0, modalY);

      this.componentCustomButton.style.left = `${modalX}px`;
      this.componentCustomButton.style.top = `${modalY}px`;
    }
  }

  connectedCallback() {
    eventBus.on(EventTypes.UpdateWorkflowSettings, this.onUpdateWorkflowSettings);
    eventBus.on(EventTypes.FlyoutPanelTabSelected, this.onFlyoutPanelTabSelected);
    eventBus.on(EventTypes.TestActivityMessageReceived, this.onTestActivityMessageReceived);
    eventBus.on(EventTypes.UpdateActivity, this.onUpdateActivity);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.UpdateWorkflowSettings, this.onUpdateWorkflowSettings);
    eventBus.detach(EventTypes.FlyoutPanelTabSelected, this.onFlyoutPanelTabSelected);
    eventBus.detach(EventTypes.TestActivityMessageReceived, this.onTestActivityMessageReceived);
    eventBus.detach(EventTypes.UpdateActivity, this.onUpdateActivity);
  }

  async configureComponentCustomButton(message: WorkflowTestActivityMessage) {
    this.configureComponentCustomButtonContext = {
      component: 'elsa-workflow-definition-editor-screen',
      activityType: message.activityType,
      prop: null,
      data: null,
    };
    await eventBus.emit(EventTypes.ComponentLoadingCustomButton, this, this.configureComponentCustomButtonContext);
  }

  t = (key: string) => this.i18next.t(key);

  async loadActivityDescriptors() {
    const client = await createElsaClient(this.serverUrl);
    state.activityDescriptors = await client.activitiesApi.list();
  }

  async loadWorkflowStorageDescriptors() {
    const client = await createElsaClient(this.serverUrl);
    state.workflowStorageDescriptors = await client.workflowStorageProvidersApi.list();
  }

  async tryUpdateActivityInformation(activityId: string) {
    if (!this.workflowInstanceId) {
      this.activityStats = null;
      this.workflowInstance = null;

      return;
    }

    const client = await createElsaClient(this.serverUrl);

    this.activityStats = await client.activityStatsApi.get(this.workflowInstanceId, activityId);

    if (!this.workflowInstance || this.workflowInstance.id !== this.workflowInstanceId) this.workflowInstance = await client.workflowInstancesApi.get(this.workflowInstanceId);
  }

  updateWorkflowDefinition(value: WorkflowDefinition) {
    this.workflowDefinition = value;
    this.workflowModel = this.mapWorkflowModel(value);
  }

  async publishWorkflow() {
    this.publishing = true;
    await this.saveWorkflow(true);
    this.publishing = false;
    await eventBus.emit(EventTypes.WorkflowPublished, this, this.workflowDefinition);
  }

  async unpublishWorkflow() {
    await this.unpublishWorkflowInternal();
    await eventBus.emit(EventTypes.WorkflowRetracted, this, this.workflowDefinition);
  }

  async revertWorkflow() {
    await this.revertWorkflowInternal();
  }

  async saveWorkflow(publish?: boolean) {
    await this.saveWorkflowInternal(null, publish);
  }

  async saveWorkflowInternal(workflowModel?: WorkflowModel, publish?: boolean) {
    if (!this.serverUrl || this.serverUrl.length == 0) return;

    workflowModel = workflowModel || this.workflowModel;

    const client = await createElsaClient(this.serverUrl);
    let workflowDefinition = this.workflowDefinition;
    const isNew = typeof workflowDefinition.definitionId === 'undefined' && typeof this.workflowDefinitionId === 'undefined';

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
        x: x.x,
        y: x.y,
        persistWorkflow: x.persistWorkflow,
        loadWorkflowContext: x.loadWorkflowContext,
        saveWorkflowContext: x.saveWorkflowContext,
        properties: x.properties,
        propertyStorageProviders: x.propertyStorageProviders,
        category: '',
      })),
      connections: workflowModel.connections.map<ConnectionDefinition>(x => ({
        sourceActivityId: x.sourceId,
        targetActivityId: x.targetId,
        outcome: x.outcome,
      })),
    };

    this.saving = !publish;
    this.publishing = publish;

    try {
      console.debug('Saving workflow...');

      workflowDefinition = await client.workflowDefinitionsApi.save(request);
      this.workflowDefinition = workflowDefinition;
      this.workflowModel = this.mapWorkflowModel(workflowDefinition);

      this.saving = false;
      this.saved = !publish;
      this.publishing = false;
      setTimeout(() => (this.saved = false), 500);
      this.workflowSaved.emit(workflowDefinition);
      if (isNew) {
        this.updateUrl(workflowDefinition.definitionId);
      }
    } catch (e) {
      console.error(e);
      this.saving = false;
      this.saved = false;
      this.networkError = e.message;
      setTimeout(() => (this.networkError = null), 10000);
    }
  }

  async unpublishWorkflowInternal() {
    const client = await createElsaClient(this.serverUrl);
    const workflowDefinitionId = this.workflowDefinition.definitionId;
    this.unPublishing = true;

    try {
      this.workflowDefinition = await client.workflowDefinitionsApi.retract(workflowDefinitionId);
      this.unPublishing = false;
      this.unPublished = true;
      setTimeout(() => (this.unPublished = false), 500);
    } catch (e) {
      console.error(e);
      this.unPublishing = false;
      this.unPublished = false;
      this.networkError = e.message;
      setTimeout(() => (this.networkError = null), 2000);
    }
  }

  async revertWorkflowInternal() {
    const client = await createElsaClient(this.serverUrl);
    const workflowDefinitionId = this.workflowDefinition.definitionId;
    const version = this.workflowDefinition.version;
    this.reverting = true;

    try {
      this.workflowDefinition = await client.workflowDefinitionsApi.revert(workflowDefinitionId, version);
      this.reverting = false;
      this.reverted = true;
      setTimeout(() => (this.reverted = false), 500);
    } catch (e) {
      console.error(e);
      this.reverting = false;
      this.reverted = false;
      this.networkError = e.message;
      setTimeout(() => (this.networkError = null), 2000);
    }
  }

  updateUrl(id) {
    if (this.history) {
      this.history.push(`${this.basePath}/workflow-definitions/${id}`, {});
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
      x: source.x,
      y: source.y,
      name: source.name,
      type: source.type,
      properties: source.properties,
      outcomes: [...outcomes],
      persistWorkflow: source.persistWorkflow,
      saveWorkflowContext: source.saveWorkflowContext,
      loadWorkflowContext: source.loadWorkflowContext,
      propertyStorageProviders: source.propertyStorageProviders,
    };
  }

  mapConnectionModel(source: ConnectionDefinition): ConnectionModel {
    return {
      sourceId: source.sourceActivityId,
      targetId: source.targetActivityId,
      outcome: source.outcome,
    };
  }

  async handleContextMenuChange(state: ActivityContextMenuState) {
    this.activityContextMenuState = state;
  }

  // handleConnectionContextMenuChange(state: ActivityContextMenuState) {
  //   this.connectionContextMenuState = state;
  // }

  handleContextMenuTestChange(x: number, y: number, shown: boolean, activity: ActivityModel) {
    this.activityContextMenuTestState = {
      shown,
      x,
      y,
      activity,
    };
  }

  async onShowWorkflowSettingsClick() {
    await eventBus.emit(EventTypes.ShowWorkflowSettings);
  }

  async onDeleteClicked() {
    const t = this.t;
    const result = await this.confirmDialog.show(t('DeleteConfirmationModel.Title'), t('DeleteConfirmationModel.Message'));

    if (!result)
      return;

    const elsaClient = await createElsaClient(this.serverUrl);
    await elsaClient.workflowDefinitionsApi.delete(this.workflowDefinition.definitionId, {allVersions: true});
    this.history.push(`${this.basePath}/workflow-definitions`, {});
  }

  async onPublishClicked() {
    await this.publishWorkflow();
  }

  async onUnPublishClicked() {
    await this.unpublishWorkflow();
  }

  async onRevertClicked() {
    await this.revertWorkflow();
  }

  async onExportClicked() {
    await this.exportWorkflow();
  }

  async onImportClicked(file: File) {
    await this.importWorkflow(file);
  }

  async onDeleteActivityClick(e: Event) {
    e.preventDefault();
    const { activity, selectedActivities } = this.activityContextMenuState;

    if (selectedActivities[activity.activityId]) {
      await this.designer.removeSelectedActivities();
    } else {
      await this.designer.removeActivity(activity);
    }

    this.handleContextMenuChange({ x: 0, y: 0, shown: false, activity: null, selectedActivities: {} });
    await eventBus.emit(EventTypes.HideModalDialog);
  }

  async onEditActivityClick(e: Event) {
    e.preventDefault();
    await this.designer.showActivityEditor(this.activityContextMenuState.activity, true);
    this.handleContextMenuChange({ x: 0, y: 0, shown: false, activity: null });
  }

  // async onPasteActivityClick(e: Event) {
  //   e.preventDefault();
  //   let activityModel = this.connectionContextMenuState.activity;
  //   await eventBus.emit(EventTypes.PasteActivity, this, activityModel);
  //   this.handleConnectionContextMenuChange({x: 0, y: 0, shown: false, activity: null});
  // }

  async onActivityContextMenuButtonClicked(e: CustomEvent<ActivityContextMenuState>) {
    this.activityContextMenuState = e.detail;
  }

  async onActivityContextMenuButtonTestClicked(e: CustomEvent<ActivityContextMenuState>) {

    this.activityContextMenuTestState = e.detail;
    this.selectedActivityId = e.detail.activity.activityId;

    if (!e.detail.shown) {
      return;
    }

    await this.tryUpdateActivityInformation(this.selectedActivityId);
  }

  async onActivitySelected(e: CustomEvent<ActivityModel>) {
    this.selectedActivityId = e.detail.activityId;
  }

  async onActivityDeselected(e: CustomEvent<ActivityModel>) {
    if (this.selectedActivityId == e.detail.activityId) this.selectedActivityId = null;
  }

  // onConnectionContextMenuButtonClicked(e: CustomEvent<ActivityContextMenuState>) {
  //   this.connectionContextMenuState = e.detail;
  // }

  onTestActivityMessageReceived = async args => {
    const message = args as WorkflowTestActivityMessage;

    if (!!message) {
      this.workflowInstanceId = message.workflowInstanceId;
      this.workflowTestActivityMessages = this.workflowTestActivityMessages.filter(x => x.activityId !== message.activityId);
      this.workflowTestActivityMessages = [...this.workflowTestActivityMessages, message];
    } else {
      this.workflowTestActivityMessages = [];
      this.workflowInstanceId = null;
    }

    this.render();
  };

  async onRestartActivityButtonClick() {
    await eventBus.emit(EventTypes.WorkflowRestarted, this, this.selectedActivityId);

    this.handleContextMenuTestChange(0, 0, false, null);
  }

  private onUpdateWorkflowSettings = async (workflowDefinition: WorkflowDefinition) => {
    this.updateWorkflowDefinition(workflowDefinition);
    await this.saveWorkflowInternal(this.workflowModel);
  };

  private onFlyoutPanelTabSelected = async args => {
    const tab = args;
    if (tab === 'general') this.workflowDesignerMode = WorkflowDesignerMode.Edit;
    if (tab === 'test') this.workflowDesignerMode = WorkflowDesignerMode.Test;
    this.render();
  };

  onUpdateActivity = (activity: ActivityModel) => {
    const message = this.workflowTestActivityMessages.find(x => x.activityId === activity.activityId);

    if (message) {
      message.status = WorkflowTestActivityMessageStatus.Modified;
      this.clearSubsequentWorkflowTestMessages(activity.activityId);
    }
  };

  private clearSubsequentWorkflowTestMessages(activityId: string) {
    const targetActivityId = this.workflowDefinition.connections.find(x => x.sourceActivityId === activityId)?.targetActivityId;

    if (!targetActivityId) return;

    this.workflowTestActivityMessages = this.workflowTestActivityMessages.filter(x => x.activityId !== targetActivityId || x.status === WorkflowTestActivityMessageStatus.Failed);

    this.clearSubsequentWorkflowTestMessages(targetActivityId);
  }

  renderActivityStatsButton = (activity: ActivityModel): string => {

    const testActivityMessage = this.workflowTestActivityMessages.find(x => x.activityId == activity.activityId);
    if (testActivityMessage == undefined) return '';

    let icon: string;

    switch (testActivityMessage.status) {
      case WorkflowTestActivityMessageStatus.Done:
      default:
        icon = `<svg class="elsa-h-8 elsa-w-8 elsa-text-green-500"  fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>`;
        break;
      case WorkflowTestActivityMessageStatus.Waiting:
        icon = `<svg version="1.1" class="svg-loader" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px" viewBox="0 0 80 80" xml:space="preserve">
                  <path id="spinner" fill="#7eb0de" d="M40,72C22.4,72,8,57.6,8,40C8,22.4,
                  22.4,8,40,8c17.6,0,32,14.4,32,32c0,1.1-0.9,2-2,2
                  s-2-0.9-2-2c0-15.4-12.6-28-28-28S12,24.6,12,40s12.6,
                  28,28,28c1.1,0,2,0.9,2,2S41.1,72,40,72z">
                    <animateTransform attributeType="xml" attributeName="transform" type="rotate" from="0 40 40" to="360 40 40" dur="0.75s" repeatCount="indefinite" />
                  </path>
                  </path>
              </svg>`;
        break;
      case WorkflowTestActivityMessageStatus.Failed:
        icon = `<svg class="elsa-h-8 elsa-w-8 elsa-text-red-500"  viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="15" y1="9" x2="9" y2="15" />
                  <line x1="9" y1="9" x2="15" y2="15" />
                </svg>`;
        break;
      case WorkflowTestActivityMessageStatus.Modified:
        icon = `<svg class="h-6 w-6 elsa-text-yellow-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"></circle>
                    <line x1="12" y1="16" x2="12" y2="12"></line>
                    <line x1="12" y1="8" x2="12.01" y2="8"></line>
                </svg>`;
        break;
    }

    return `<div class="context-menu-wrapper elsa-flex-shrink-0">
            <button aria-haspopup="true"
                    class="elsa-w-8 elsa-h-8 elsa-inline-flex elsa-items-center elsa-justify-center elsa-text-gray-400 elsa-rounded-full elsa-bg-transparent hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-text-gray-500 focus:elsa-bg-gray-100 elsa-transition elsa-ease-in-out elsa-duration-150">
              ${icon}
            </button>
          </div>`;
  };

  renderMonacoEditorDialog() {
    return (
      <elsa-modal-dialog ref={el => {
          monacoEditorDialogService.monacoEditorDialog = el;
        }}>
          <div slot="content" class="elsa-py-8 elsa-px-4">
            <elsa-monaco
              value=""
              language="javascript"
              editor-height="400px"
              single-line={false}
              onValueChanged={e => {
                monacoEditorDialogService.currentValue = e.detail.value;
              }}
              ref={el => (monacoEditorDialogService.monacoEditor = el)}
            />
          </div>
          <div slot="buttons">
            <div class="elsa-bg-gray-50 elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
              <button
                type="button"
                onClick={() => {
                  monacoEditorDialogService.save();
                  monacoEditorDialogService.monacoEditorDialog.hide();
                }}
                class="elsa-ml-3 elsa-inline-flex elsa-justify-center elsa-py-2 elsa-px-4 elsa-border elsa-border-transparent elsa-shadow-sm elsa-text-sm elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-blue-600 hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500"
              >
                Save
              </button>
              <button
                type="button"
                onClick={() => {
                  monacoEditorDialogService.monacoEditorDialog.hide();
                }}
                class="elsa-mt-3 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-white elsa-text-base elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-mt-0 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm"
              >
                Cancel
              </button>
            </div>
          </div>
        </elsa-modal-dialog>
    );
  }

  render() {
    const tunnelState: WorkflowEditorState = {
      serverUrl: this.serverUrl,
      workflowDefinitionId: this.workflowDefinition.definitionId,
      serverFeatures: this.serverFeatures,
    };

    return (
      <Host class="elsa-flex elsa-flex-col elsa-w-full" ref={el => (this.el = el)}>
        <WorkflowEditorTunnel.Provider state={tunnelState}>
          {this.renderCanvas()}
          {this.renderActivityPicker()}
          {this.renderActivityEditor()}
          {this.renderMonacoEditorDialog()}
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
        {!state.useX6Graphs && (
          <elsa-designer-tree
            model={this.workflowModel}
            mode={this.workflowDesignerMode}
            layoutDirection={this.layoutDirection}
            activityContextMenuButton={this.workflowDesignerMode == WorkflowDesignerMode.Edit ? activityContextMenuButton : this.renderActivityStatsButton}
            onActivityContextMenuButtonClicked={e => this.onActivityContextMenuButtonClicked(e)}
            onActivityContextMenuButtonTestClicked={e => this.onActivityContextMenuButtonTestClicked(e)}
            activityContextMenu={this.workflowDesignerMode == WorkflowDesignerMode.Edit ? this.activityContextMenuState : this.activityContextMenuTestState}
            enableMultipleConnectionsFromSingleSource={false}
            selectedActivityIds={[this.selectedActivityId]}
            onActivitySelected={e => this.onActivitySelected(e)}
            onActivityDeselected={e => this.onActivityDeselected(e)}
            class="elsa-flex-1"
            ref={el => (this.designer = el)}
          />
        )}
        {state.useX6Graphs && (
          <x6-designer
            model={this.workflowModel}
            mode={this.workflowDesignerMode}
            layoutDirection={this.layoutDirection}
            activityContextMenuButton={this.workflowDesignerMode == WorkflowDesignerMode.Edit ? (() => '') : this.renderActivityStatsButton}
            onActivityContextMenuButtonClicked={e => this.onActivityContextMenuButtonClicked(e)}
            onActivityContextMenuButtonTestClicked={e => this.onActivityContextMenuButtonTestClicked(e)}
            activityContextMenu={this.workflowDesignerMode == WorkflowDesignerMode.Edit ? this.activityContextMenuState : this.activityContextMenuTestState}
            enableMultipleConnectionsFromSingleSource={false}
            selectedActivityIds={[this.selectedActivityId]}
            onActivitySelected={e => this.onActivitySelected(e)}
            onActivityDeselected={e => this.onActivityDeselected(e)}
            class="elsa-workflow-wrapper"
            ref={el => (this.designer = el)}
          />
        )}

        {this.renderWorkflowSettingsButton()}
        {this.renderWorkflowHelpButton()}
        {this.renderWorkflowPanel()}
        {this.renderActivityContextMenu()}
        {/*{this.renderConnectionContextMenu()}*/}
        <elsa-workflow-settings-modal workflowDefinition={this.workflowDefinition} />
        <elsa-workflow-definition-editor-notifications />
        <div class="elsa-fixed elsa-bottom-10 elsa-right-12">
          <div class="elsa-flex elsa-items-center elsa-space-x-4">
            {this.renderSavingIndicator()}
            {this.renderNetworkError()}
            {this.renderPublishButton()}
          </div>
        </div>
        {this.renderTestActivityMenu()}
        <elsa-confirm-dialog ref={el => this.confirmDialog = el} culture={this.culture}/>
      </div>
    );
  }

  async onComponentCustomButtonClick(message: WorkflowTestActivityMessage) {
    let workflowModel = { ...this.workflowModel };
    const activityModel = workflowModel.activities.find(x => x.activityId == message.activityId);
    const input = message.data['Input'];

    const componentCustomButtonClickContext: ComponentCustomButtonClickContext = {
      component: 'elsa-workflow-definition-editor-screen',
      activityType: message.activityType,
      prop: null,
      params: [activityModel, input],
    };
    await eventBus.emit(EventTypes.ComponentCustomButtonClick, this, componentCustomButtonClickContext);
  }

  private canBeRestartedFromCurrentActivity() {
    if (!this.selectedActivityId) return false;
    if (this.workflowDesignerMode !== WorkflowDesignerMode.Test) return false;

    if (this.workflowTestActivityMessages.some(x => x.activityId === this.selectedActivityId)) return true;

    const sourceActivityId = this.workflowDefinition.connections.find(x => x.targetActivityId === this.selectedActivityId)?.sourceActivityId;

    return sourceActivityId && this.workflowTestActivityMessages.some(x => x.activityId === sourceActivityId && x.status !== WorkflowTestActivityMessageStatus.Failed);
  }

  renderTestActivityMenu = () => {
    const message = this.workflowTestActivityMessages.find(x => x.activityId == this.selectedActivityId);

    const renderActivityTestError = () => {
      if (message == undefined || !message) return;

      if (!message.error) return;

      return (
        <div class="elsa-ml-4">
          <elsa-workflow-fault-information
            workflowFault={this.workflowInstance?.faults.find(x => x.faultedActivityId == this.selectedActivityId)}
            faultedAt={this.workflowInstance?.faultedAt}
          />
        </div>
      );
    };

    const renderPerformanceStats = () => {
      if (!!message.error) return;

      return (
        <div class="elsa-ml-4">
          <elsa-workflow-performance-information activityStats={this.activityStats} />
        </div>
      );
    };

    const renderRestartButton = () => {
      if (!this.canBeRestartedFromCurrentActivity()) return undefined;

      return (
        <button
          type="button"
          onClick={() => this.onRestartActivityButtonClick()}
          class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-red-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-red-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm"
        >
          {this.t('Restart')}
        </button>
      );
    };

    const renderMessage = () => {
      const t = this.t;
      if (message == undefined || !message) return;

      this.configureComponentCustomButton(message);

      const filteredData = {};
      const wellKnownDataKeys = { State: true, Input: null, Outcomes: true, Exception: true };
      let dataKey = null;

      for (const key in message.data) {
        if (!message.data.hasOwnProperty(key)) continue;

        if (!!wellKnownDataKeys[key]) continue;

        const value = message.data[key];

        if (!value && value != 0) continue;

        let valueText = null;
        dataKey = key;

        if (typeof value == 'string') valueText = value;
        else if (typeof value == 'object') valueText = JSON.stringify(value, null, 1);
        else if (typeof value == 'undefined') valueText = null;
        else valueText = value.toString();

        filteredData[key] = valueText;
      }

      const hasBody = !!message.data?.Input?.Body;

      return (
        <div class="elsa-relative elsa-grid elsa-gap-6 elsa-bg-white px-5 elsa-py-6 sm:elsa-gap-8 sm:elsa-p-8">
          <div class="elsa-flex elsa-flex-row elsa-justify-between">
            <div class="elsa-ml-4">
              <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">{t('Status')}</p>
              <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">{message.status}</p>
            </div>
            <div>{renderRestartButton()}</div>
          </div>
          {collection.map(filteredData, (v, k) => (
            <div class="elsa-ml-4">
              <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">{k}</p>
              <pre class="elsa-mt-1 elsa-text-sm elsa-text-gray-500 elsa-overflow-x-auto" style={{ "max-width": "30rem"}}>{v}</pre>
            </div>
          ))}
          {hasBody ? renderComponentCustomButton() : undefined}
          {renderActivityTestError()}
          {renderPerformanceStats()}
        </div>
      );
    };

    const renderComponentCustomButton = () => {
      if (this.configureComponentCustomButtonContext.data == null) return;

      const label = this.configureComponentCustomButtonContext.data.label;

      return (
        <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
          <button
            type="button"
            onClick={() => this.onComponentCustomButtonClick(message)}
            class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm"
          >
            {label}
          </button>
        </div>
      );
    };

    const renderLoader = function () {
      return <div class="elsa-p-6 elsa-bg-white">Loading...</div>;
    };

    return (
      <div
        data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
        data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
        data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
        data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
        data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
        data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
        class={`${this.activityContextMenuTestState.shown ? '' : 'hidden'} elsa-absolute elsa-z-10 elsa-mt-3 elsa-px-2 elsa-w-screen elsa-max-w-xl sm:elsa-px-0`}
        style={{
          left: `${this.activityContextMenuTestState.x + 64}px`,
          top: `${this.activityContextMenuTestState.y - 256}px`,
        }}
        ref={el => (this.componentCustomButton = el)}
      >
        <div class="elsa-rounded-lg elsa-shadow-lg elsa-ring-1 elsa-ring-black elsa-ring-opacity-5 elsa-overflow-x-hidden elsa-overflow-y-auto" style={{ "max-height": "700px" }}>{!!message ? renderMessage() : renderLoader()}</div>
      </div>
    );
  };

  renderActivityContextMenu() {
    const t = this.t;
    const selectedActivities = Object.keys(this.activityContextMenuState.selectedActivities ?? {});
    const { activity } = this.activityContextMenuState;

    return (
      <div
        data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
        data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
        data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
        data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
        data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
        data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
        class={`${this.activityContextMenuState.shown ? '' : 'hidden'} context-menu elsa-z-20 elsa-mx-3 elsa-w-48 elsa-mt-1 elsa-rounded-md elsa-shadow-lg elsa-fixed`}
        style={{ left: `${this.activityContextMenuState.x}px`, top: `${this.activityContextMenuState.y}px` }}
        ref={el => (this.activityContextMenu = el)}
      >
        <div class="elsa-rounded-md elsa-bg-white elsa-shadow-xs" role="menu" aria-orientation="vertical" aria-labelledby="pinned-project-options-menu-0">
          <div class="elsa-py-1">
            <a
              onClick={e => this.onEditActivityClick(e)}
              href="#"
              class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-leading-5 elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900 focus:elsa-outline-none focus:elsa-bg-gray-100 focus:elsa-text-gray-900"
              role="menuitem"
            >
              {t('ActivityContextMenu.Edit')}
            </a>
          </div>
          <div class="elsa-border-t elsa-border-gray-100" />
          <div class="elsa-py-1">
            <a
              onClick={e => this.onDeleteActivityClick(e)}
              href="#"
              class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-leading-5 elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900 focus:elsa-outline-none focus:elsa-bg-gray-100 focus:elsa-text-gray-900"
              role="menuitem"
            >
              {selectedActivities.length > 1 && selectedActivities.indexOf(activity.activityId) !== -1 ? t('ActivityContextMenu.DeleteSelected') : t('ActivityContextMenu.Delete')}
            </a>
          </div>
        </div>
      </div>
    );
  }

  // renderConnectionContextMenu() {
  //   const t = this.t;
  //
  //   return <div
  //     data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
  //     data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
  //     data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
  //     data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
  //     data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
  //     data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
  //     class={`${this.connectionContextMenuState.shown ? '' : 'hidden'} context-menu elsa-z-10 elsa-mx-3 elsa-w-48 elsa-mt-1 elsa-rounded-md elsa-shadow-lg elsa-absolute`}
  //     style={{left: `${this.connectionContextMenuState.x}px`, top: `${this.connectionContextMenuState.y - 64}px`}}
  //     ref={el => this.connectionContextMenu = el}
  //   >
  //     <div class="elsa-rounded-md elsa-bg-white elsa-shadow-xs" role="menu" aria-orientation="vertical"
  //          aria-labelledby="pinned-project-options-menu-0">
  //       <div class="elsa-py-1">
  //         <a
  //           onClick={e => this.onPasteActivityClick(e)}
  //           href="#"
  //           class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-leading-5 elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900 focus:elsa-outline-none focus:elsa-bg-gray-100 focus:elsa-text-gray-900"
  //           role="menuitem">
  //           {t('ConnectionContextMenu.Paste')}
  //         </a>
  //       </div>
  //     </div>
  //   </div>
  // }

  renderActivityPicker() {
    return <elsa-activity-picker-modal />;
  }

  renderActivityEditor() {
    return <elsa-activity-editor-modal culture={this.culture} />;
  }

  renderWorkflowSettingsButton() {
    return (
      <button
        onClick={() => this.onShowWorkflowSettingsClick()}
        type="button"
        class="workflow-settings-button elsa-fixed elsa-top-20 elsa-right-12 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke="currentColor" fill="none" class="elsa-h-8 elsa-w-8">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
          />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
      </button>
    );
  }

  renderWorkflowHelpButton() {
    return (
      <span>
        <button
          type="button"
          onClick={this.showHelpModal}
          class="workflow-settings-button elsa-fixed elsa-top-20 elsa-right-28 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-8 elsa-w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </button>
        <elsa-modal-dialog ref={el => (this.helpDialog = el)}>
          {!state.useX6Graphs && (
            <div slot="content" class="elsa-p-8">
              <h3 class="elsa-text-lg elsa-font-medium">Actions</h3>
              <dl class="elsa-mt-2 elsa-border-t elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Delete connections</dt>
                  <dd class="elsa-text-gray-900">RIGHT-click the connection to delete.</dd>
                </div>
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Connect outcomes to existing activity</dt>
                  <dd class="elsa-text-gray-900">Press and hold SHIFT while LEFT-clicking the outcome to connect. Release SHIFT and LEFT-click the target activity.</dd>
                </div>
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Pan</dt>
                  <dd class="elsa-text-gray-900">Click anywhere on the designer and drag mouse.</dd>
                </div>
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Zoom</dt>
                  <dd class="elsa-text-gray-900">Use scroll-wheel on mouse.</dd>
                </div>
              </dl>
            </div>
          )}
          {state.useX6Graphs && (
            <div slot="content" class="elsa-p-8">
              <h3 class="elsa-text-lg elsa-font-medium">Actions</h3>
              <dl class="elsa-mt-2 elsa-border-t elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Delete connections</dt>
                  <dd class="elsa-text-gray-900">Hover on the connection and click the red X.</dd>
                </div>
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Edit activities</dt>
                  <dd class="elsa-text-gray-900">Right click on the activity node and click Edit.</dd>
                </div>
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Pan</dt>
                  <dd class="elsa-text-gray-900">Hold Ctrl, click anywhere on the designer and drag mouse.</dd>
                </div>
                <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                  <dt class="elsa-text-gray-500">Zoom</dt>
                  <dd class="elsa-text-gray-900">Use scroll-wheel on mouse while holding Ctrl.</dd>
                </div>
              </dl>
            </div>
          )}
        </elsa-modal-dialog>
      </span>
    );
  }

  showHelpModal = async () => {
    await this.helpDialog.show();
  };

  renderSavingIndicator() {
    if (this.publishing) return undefined;

    const t = this.t;
    const message = this.unPublishing
      ? t('Unpublishing...')
      : this.unPublished
      ? t('Unpublished')
      : this.saving
      ? 'Saving...'
      : this.saved
      ? 'Saved'
      : this.importing
      ? 'Importing...'
      : this.imported
      ? 'Imported'
      : null;

    if (!message) return undefined;

    return (
      <div>
        <span class="elsa-text-gray-400 elsa-text-sm">{message}</span>
      </div>
    );
  }

  renderNetworkError() {
    if (!this.networkError) return undefined;

    return (
      <div>
        <span class="elsa-text-rose-400 elsa-text-sm">An error occurred: {this.networkError}</span>
      </div>
    );
  }

  renderPublishButton() {
    return (
      <elsa-workflow-publish-button
        publishing={this.publishing}
        workflowDefinition={this.workflowDefinition}
        onPublishClicked={() => this.onPublishClicked()}
        onUnPublishClicked={() => this.onUnPublishClicked()}
        onRevertClicked={() => this.onRevertClicked()}
        onExportClicked={() => this.onExportClicked()}
        onImportClicked={e => this.onImportClicked(e.detail)}
        onDeleteClicked={e => this.onDeleteClicked()}
        culture={this.culture}
      />
    );
  }

  private static createWorkflowDefinition(): WorkflowDefinition {
    return {
      definitionId: null,
      version: 1,
      isLatest: true,
      isPublished: false,
      activities: [],
      connections: [],
      persistenceBehavior: WorkflowPersistenceBehavior.WorkflowBurst,
    };
  }

  private renderWorkflowPanel() {
    const workflowDefinition = this.workflowDefinition;

    return (
      <elsa-flyout-panel expandButtonPosition={3}>
        <elsa-tab-header tab="general" slot="header">
          General
        </elsa-tab-header>
        <elsa-tab-content tab="general" slot="content">
          <elsa-workflow-properties-panel workflowDefinition={workflowDefinition} />
        </elsa-tab-content>
        {this.renderTestPanel()}
        {this.renderDesignerPanel()}
        {this.renderVersionHistoryPanel(workflowDefinition)}
      </elsa-flyout-panel>
    );
  }

  private renderTestPanel() {
    const testingEnabled = this.serverFeatures.find(x => x == 'WorkflowTesting');

    if (!testingEnabled) return;

    return [
      <elsa-tab-header tab="test" slot="header">
        Test
      </elsa-tab-header>,
      <elsa-tab-content tab="test" slot="content">
        <elsa-workflow-test-panel workflowDefinition={this.workflowDefinition} workflowTestActivityId={this.selectedActivityId} selectedActivityId={this.selectedActivityId}/>
      </elsa-tab-content>,
    ];
  }

  private renderDesignerPanel = () => {
    const isFeaturePanelVisible = featuresDataManager.getUIFeatureList().length != 0;

    if (isFeaturePanelVisible) {
      return [
        <elsa-tab-header tab="designer" slot="header">
          Designer
        </elsa-tab-header>,
        <elsa-tab-content tab="designer" slot="content">
          <elsa-designer-panel onFeatureChanged={this.handleFeatureChange} onFeatureStatusChanged={this.handleFeatureStatusChange} />
        </elsa-tab-content>,
      ];
    }
  };

  private renderVersionHistoryPanel = (workflowDefinition: WorkflowDefinition) => {
    return [
      <elsa-tab-header tab="versionHistory" slot="header">
        Version History
      </elsa-tab-header>,
      <elsa-tab-content tab="versionHistory" slot="content">
        <elsa-version-history-panel
          workflowDefinition={workflowDefinition}
          onVersionSelected={e => this.onVersionSelected(e)}
          onDeleteVersionClicked={e => this.onDeleteVersionClicked(e)}
          onRevertVersionClicked={e => this.onRevertVersionClicked(e)}
        />
      </elsa-tab-content>,
    ];
  };

  handleFeatureChange = (e: CustomEvent<string>) => {
    const feature = e.detail;

    if (feature === featuresDataManager.supportedFeatures.workflowLayout) {
      const layoutFeature = featuresDataManager.getFeatureConfig(feature);
      this.layoutDirection = layoutFeature.value as LayoutDirection;
    }
  };

  handleFeatureStatusChange = (e: CustomEvent<string>) => {
    const feature = e.detail;

    if (feature === featuresDataManager.supportedFeatures.workflowLayout) {
      const layoutFeature = featuresDataManager.getFeatureConfig(feature);
      if (layoutFeature.enabled) {
        this.layoutDirection = layoutFeature.value as LayoutDirection;
      } else {
        this.layoutDirection = LayoutDirection.TopBottom;
      }
    }
  };

  onVersionSelected = async (e: CustomEvent<WorkflowDefinitionVersion>) => {
    const client = await createElsaClient(this.serverUrl);
    const version = e.detail;
    const workflowDefinition = await client.workflowDefinitionsApi.getByDefinitionAndVersion(version.definitionId, { version: version.version });
    this.updateWorkflowDefinition(workflowDefinition);
  };

  onDeleteVersionClicked = async (e: CustomEvent<WorkflowDefinitionVersion>) => {
    const client = await createElsaClient(this.serverUrl);
    const version = e.detail;
    await client.workflowDefinitionsApi.delete(version.definitionId, { version: version.version });
    this.updateWorkflowDefinition({ ...this.workflowDefinition }); // Force a rerender.
  };

  onRevertVersionClicked = async (e: CustomEvent<WorkflowDefinitionVersion>) => {
    const client = await createElsaClient(this.serverUrl);
    const version = e.detail;
    const workflowDefinition = await client.workflowDefinitionsApi.revert(version.definitionId, version.version);
    this.updateWorkflowDefinition(workflowDefinition);
  };
}

injectHistory(ElsaWorkflowDefinitionEditorScreen);
DashboardTunnel.injectProps(ElsaWorkflowDefinitionEditorScreen, ['serverUrl', 'culture', 'monacoLibPath', 'basePath', 'serverFeatures']);
