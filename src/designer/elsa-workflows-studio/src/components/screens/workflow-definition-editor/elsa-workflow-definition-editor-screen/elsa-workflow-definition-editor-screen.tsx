import {Component, Event, EventEmitter, h, Host, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {RouterHistory, injectHistory} from '@stencil/router';
import {
  ActivityDefinition,
  ActivityDescriptor,
  ActivityModel, 
  ConfigureComponentCustomButtonContext, 
  ComponentCustomButtonClickContext, 
  ConnectionDefinition,
  ConnectionModel,
  EventTypes,
  VersionOptions,
  WorkflowDefinition,
  WorkflowModel,
  WorkflowPersistenceBehavior,
  WorkflowTestActivityMessage
} from "../../../../models";
import {createElsaClient, eventBus, SaveWorkflowDefinitionRequest} from "../../../../services";
import state from '../../../../utils/store';
import WorkflowEditorTunnel, {WorkflowEditorState} from '../../../../data/workflow-editor';
import DashboardTunnel from "../../../../data/dashboard";
import {downloadFromBlob} from "../../../../utils/download";
import {ActivityContextMenuState, WorkflowDesignerMode} from "../../../designers/tree/elsa-designer-tree/models";
import {registerClickOutside} from "stencil-click-outside";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import * as collection from 'lodash/collection';
import {Map} from "../../../../utils/utils";

@Component({
  tag: 'elsa-workflow-definition-editor-screen',
  styleUrl: 'elsa-workflow-definition-editor-screen.css',
  shadow: false
})
export class ElsaWorkflowDefinitionEditorScreen {

  @Event() workflowSaved: EventEmitter<WorkflowDefinition>;
  @Prop({attribute: 'workflow-definition-id', reflect: true}) workflowDefinitionId: string;
  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;
  @Prop() culture: string;
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
  @State() networkError: string;
  @State() selectedActivityId?: string;
  @State() workflowDesignerMode: WorkflowDesignerMode;
  @State() workflowTestActivityMessages: Array<WorkflowTestActivityMessage> = [];

  @State() activityContextMenuState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
    selectedActivities: {}
  };

  @State() connectionContextMenuState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
  };

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
    const versionOptions: VersionOptions = {version: workflowDefinition.version};
    const response = await client.workflowDefinitionsApi.export(workflowDefinition.definitionId, versionOptions);
    downloadFromBlob(response.data, {contentType: 'application/json', fileName: response.fileName});
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
      this.updateUrl(workflowDefinition.definitionId)

      this.importing = false;
      this.imported = true;
      setTimeout(() => this.imported = false, 500);
      await eventBus.emit(EventTypes.WorkflowImported, this, this.workflowDefinition);
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
    const client = await createElsaClient(this.serverUrl);

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
    this.workflowDesignerMode = WorkflowDesignerMode.Edit;
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
    eventBus.on(EventTypes.FlyoutPanelTabSelected, this.onFlyoutPanelTabSelected);
    eventBus.on(EventTypes.TestActivityMessageReceived, this.onTestActivityMessageReceived);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.UpdateWorkflowSettings, this.onUpdateWorkflowSettings);
    eventBus.detach(EventTypes.FlyoutPanelTabSelected, this.onFlyoutPanelTabSelected);
    eventBus.detach(EventTypes.TestActivityMessageReceived, this.onTestActivityMessageReceived);
  }

  async configureComponentCustomButton(message: WorkflowTestActivityMessage) {
    this.configureComponentCustomButtonContext = {
      component: 'elsa-workflow-definition-editor-screen',
      activityType: message.activityType,
      prop: null,
      data: null
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

  async unPublishWorkflow() {
    this.unPublishing = true;
    await this.unpublishWorkflow();
    this.unPublishing = false;
    await eventBus.emit(EventTypes.WorkflowRetracted, this, this.workflowDefinition);
  }

  async saveWorkflow(publish?: boolean) {
    await this.saveWorkflowInternal(null, publish);
  }

  async saveWorkflowInternal(workflowModel?: WorkflowModel, publish?: boolean) {
    if (!this.serverUrl || this.serverUrl.length == 0)
      return;

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
      if (isNew) {
        this.updateUrl(workflowDefinition.definitionId);
      }
    } catch (e) {
      console.error(e);
      this.saving = false;
      this.saved = false;
      this.networkError = e.message;
      setTimeout(() => this.networkError = null, 10000);
    }
  }

  async unpublishWorkflow() {
    const client = await createElsaClient(this.serverUrl);
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

  updateUrl(id) {
    this.history.push(`/workflow-definitions/${id}`, {});
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

  handleConnectionContextMenuChange(state: ActivityContextMenuState) {
    this.connectionContextMenuState = state;
  }

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
    const {activity, selectedActivities} = this.activityContextMenuState;

    if (selectedActivities[activity.activityId]) {
      await this.designer.removeSelectedActivities();
    } else {
      await this.designer.removeActivity(activity);
    }

    this.handleContextMenuChange({x: 0, y: 0, shown: false, activity: null, selectedActivities: {}});
    await eventBus.emit(EventTypes.HideModalDialog);
  }

  async onEditActivityClick(e: Event) {
    e.preventDefault();
    await this.designer.showActivityEditor(this.activityContextMenuState.activity, true);
    this.handleContextMenuChange({x: 0, y: 0, shown: false, activity: null});
  }

  async onPasteActivityClick(e: Event) {
    e.preventDefault();
    let activityModel = this.connectionContextMenuState.activity;
    await eventBus.emit(EventTypes.PasteActivity, this, activityModel);
    this.handleConnectionContextMenuChange({x: 0, y: 0, shown: false, activity: null});
  }

  async onActivityContextMenuButtonClicked(e: CustomEvent<ActivityContextMenuState>) {
    this.activityContextMenuState = e.detail;
  }

  async onActivityContextMenuButtonTestClicked(e: CustomEvent<ActivityContextMenuState>) {
    this.activityContextMenuTestState = e.detail;
    this.selectedActivityId = e.detail.activity.activityId;

    if (!e.detail.shown) {
      return;
    }
  }  

  async onActivitySelected(e: CustomEvent<ActivityModel>) {
    this.selectedActivityId = e.detail.activityId;
  }

  async onActivityDeselected(e: CustomEvent<ActivityModel>) {
    if (this.selectedActivityId == e.detail.activityId)
      this.selectedActivityId = null;
  }
  
  onConnectionContextMenuButtonClicked(e: CustomEvent<ActivityContextMenuState>) {
    this.connectionContextMenuState = e.detail;
  }

  onTestActivityMessageReceived = async args => {
    const message = args as WorkflowTestActivityMessage;
    if (!!message) {
      this.workflowTestActivityMessages = this.workflowTestActivityMessages.filter(x => x.activityId !== message.activityId);
      this.workflowTestActivityMessages = [...this.workflowTestActivityMessages, message];
    }
    else
      this.workflowTestActivityMessages = [];

    this.render();
  };   

  private onUpdateWorkflowSettings = async (workflowDefinition: WorkflowDefinition) => {
    this.updateWorkflowDefinition(workflowDefinition);
    await this.saveWorkflowInternal(this.workflowModel);
  }

  private onFlyoutPanelTabSelected = async args => {
    const tab = args;
    if (tab === 'general')
      this.workflowDesignerMode = WorkflowDesignerMode.Edit;
    if (tab === 'test')
      this.workflowDesignerMode = WorkflowDesignerMode.Test;
    this.render();
  }

  renderActivityStatsButton = (activity: ActivityModel): string => {
 
    var testActivityMessage = this.workflowTestActivityMessages.find(x => x.activityId == activity.activityId);
    if (testActivityMessage == undefined)
      return "";

    let icon: string;

    switch (testActivityMessage.status)
    {
      case "Done":
        icon = `<svg class="elsa-h-8 elsa-w-8 elsa-text-green-500"  fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>`;
        break;
      case "Waiting":
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
      case "Failed":
        icon = `<svg class="elsa-h-8 elsa-w-8 elsa-text-red-500"  viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="15" y1="9" x2="9" y2="15" />
                  <line x1="9" y1="9" x2="15" y2="15" />
                </svg>`;
        break;
    }

    return `<div class="context-menu-wrapper elsa-flex-shrink-0">
            <button aria-haspopup="true"
                    class="elsa-w-8 elsa-h-8 elsa-inline-flex elsa-items-center elsa-justify-center elsa-text-gray-400 elsa-rounded-full elsa-bg-transparent hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-text-gray-500 focus:elsa-bg-gray-100 elsa-transition elsa-ease-in-out elsa-duration-150">
              ${icon}
            </button>
          </div>`;
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
      <div class="elsa-flex-1 elsa-flex elsa-relative" >
        <elsa-designer-tree model={this.workflowModel}
                            mode={this.workflowDesignerMode}
                            activityContextMenuButton={this.workflowDesignerMode == WorkflowDesignerMode.Edit 
                              ? activityContextMenuButton
                              : this.renderActivityStatsButton}
                            onActivityContextMenuButtonClicked={e => this.onActivityContextMenuButtonClicked(e)}
                            onActivityContextMenuButtonTestClicked={e => this.onActivityContextMenuButtonTestClicked(e)}
                            activityContextMenu={this.workflowDesignerMode == WorkflowDesignerMode.Edit
                              ? this.activityContextMenuState
                              : this.activityContextMenuTestState}
                            enableMultipleConnectionsFromSingleSource={false}
                            selectedActivityIds={[this.selectedActivityId]}
                            onActivitySelected={e => this.onActivitySelected(e)}
                            onActivityDeselected={e => this.onActivityDeselected(e)}
                            class="elsa-flex-1"
                            ref={el => this.designer = el}/>
        {this.renderPanel()}
        {this.renderWorkflowSettingsButton()}
        {this.renderWorkflowHelpButton()}
        {this.renderActivityContextMenu()}
        {this.renderConnectionContextMenu()}
        <elsa-workflow-settings-modal workflowDefinition={this.workflowDefinition}/>
        <elsa-workflow-definition-editor-notifications/>
        <div class="elsa-fixed elsa-bottom-10 elsa-right-12">
          <div class="elsa-flex elsa-items-center elsa-space-x-4">
            {this.renderSavingIndicator()}
            {this.renderNetworkError()}
            {this.renderPublishButton()}
          </div>
        </div>
        {this.renderTestActivityMenu()}  
      </div>
    );
  }

  async onComponentCustomButtonClick(message: WorkflowTestActivityMessage) {
    let workflowModel = {...this.workflowModel};
    const activityModel = workflowModel.activities.find(x => x.activityId == message.activityId);
    const value = message.data["Body"];

    const componentCustomButtonClickContext: ComponentCustomButtonClickContext = {
      component: 'elsa-workflow-definition-editor-screen',
      activityType: message.activityType,
      prop: null,
      params: [activityModel, value]
    };    
    eventBus.emit(EventTypes.ComponentCustomButtonClick, this, componentCustomButtonClickContext);
  }  

  renderTestActivityMenu = () => {
    const message = this.workflowTestActivityMessages.find(x => x.activityId == this.selectedActivityId);

    const renderActivityTestError = () => {

      if (message == undefined || !message)
        return    
        
      const t = (x, params?) => this.i18next.t(x, params);
  
      if (!message.error)
        return;
  
      return (
        <div class="elsa-ml-4">
          <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">
            {t('Error')}
          </p>
          <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
            {message.error}
          </p>
        </div>
      );
    } 

    const renderMessage = () => {

      if (message == undefined || !message)
        return;
        
      this.configureComponentCustomButton(message);
      
      const t = (x, params?) => this.i18next.t(x, params);
      const filteredData = {};
      const wellKnownDataKeys = {State: true, Input: null, Outcomes: true, Exception: true};
      let dataKey = null;

      for (const key in message.data) {
        if (!message.data.hasOwnProperty(key))
          continue;

        if (!!wellKnownDataKeys[key])
          continue;

        const value = message.data[key];

        if (!value && value != 0)
          continue;

        let valueText = null;
        dataKey = key;

        if (typeof value == 'string')
          valueText = value;
        else if (typeof value == 'object')
          valueText = JSON.stringify(value, null, 1);
        else if (typeof value == 'undefined')
          valueText = null;
        else
          valueText = value.toString();

        filteredData[key] = valueText;
      }

      const hasBody = dataKey === "Body";

      return (
        <div class="elsa-relative elsa-grid elsa-gap-6 elsa-bg-white px-5 elsa-py-6 sm:elsa-gap-8 sm:elsa-p-8">
          <div class="elsa-ml-4">
            <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">
              {t('Status')}
            </p>
            <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
              {message.status}
            </p>
          </div>
          {collection.map(filteredData, (v, k) => (
            <div class="elsa-ml-4">
              <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">
                {k}
              </p>
              <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500 elsa-overflow-x-auto">
                {v}
              </p>
            </div>
          ))}
          {hasBody ? renderComponentCustomButton() : undefined}
          {renderActivityTestError()}
        </div>
      );
    };
    
    const renderComponentCustomButton = () => {

      if (this.configureComponentCustomButtonContext.data == null)
      return;

      const label = this.configureComponentCustomButtonContext.data.label;

      return (
        <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
          <button type="button"
                  onClick={() => this.onComponentCustomButtonClick(message)}
                  class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
            {label}
          </button>
        </div>          
      )    
    };    

    const renderLoader = function () {
      return <div class="elsa-p-6 elsa-bg-white">Loading...</div>;
    };

    return <div
      data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
      data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
      data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
      data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
      class={`${this.activityContextMenuTestState.shown ? '' : 'hidden'} elsa-absolute elsa-z-10 elsa-mt-3 elsa-px-2 elsa-w-screen elsa-max-w-xl sm:elsa-px-0`}
      style={{left: `${this.activityContextMenuTestState.x + 64}px`, top: `${this.activityContextMenuTestState.y - 256}px`}}
      ref={el =>
        registerClickOutside(this, el, () => {
          this.handleContextMenuTestChange(0, 0, false, null);
        })
      }>
      <div class="elsa-rounded-lg elsa-shadow-lg elsa-ring-1 elsa-ring-black elsa-ring-opacity-5 elsa-overflow-hidden">
        {!!message ? renderMessage() : renderLoader()}
      </div>
    </div>
  }  

  renderActivityContextMenu() {
    const t = this.t;
    const selectedActivities = Object.keys(this.activityContextMenuState.selectedActivities ?? {});
    const {activity} = this.activityContextMenuState;

    return <div
      data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
      data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
      data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
      data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
      class={`${this.activityContextMenuState.shown ? '' : 'hidden'} context-menu elsa-z-10 elsa-mx-3 elsa-w-48 elsa-mt-1 elsa-rounded-md elsa-shadow-lg elsa-fixed`}
      style={{left: `${this.activityContextMenuState.x}px`, top: `${this.activityContextMenuState.y}px`}}
      ref={el =>
        registerClickOutside(this, el, () => {
          this.handleContextMenuChange({x: 0, y: 0, shown: false, activity: null, selectedActivities: {}});
        })
      }
    >
      <div class="elsa-rounded-md elsa-bg-white elsa-shadow-xs" role="menu" aria-orientation="vertical"
           aria-labelledby="pinned-project-options-menu-0">
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
            {(selectedActivities.length > 1 && selectedActivities.indexOf(activity.activityId) !== -1) ? t('ActivityContextMenu.DeleteSelected') : t('ActivityContextMenu.Delete')}
          </a>
        </div>
      </div>
    </div>
  }

  renderConnectionContextMenu() {
    const t = this.t;

    return <div
      data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
      data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
      data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
      data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
      class={`${this.connectionContextMenuState.shown ? '' : 'hidden'} context-menu elsa-z-10 elsa-mx-3 elsa-w-48 elsa-mt-1 elsa-rounded-md elsa-shadow-lg elsa-absolute`}
      style={{left: `${this.connectionContextMenuState.x}px`, top: `${this.connectionContextMenuState.y - 64}px`}}
      ref={el =>
        registerClickOutside(this, el, () => {
          this.handleConnectionContextMenuChange({x: 0, y: 0, shown: false, activity: null});
        })
      }
    >
      <div class="elsa-rounded-md elsa-bg-white elsa-shadow-xs" role="menu" aria-orientation="vertical"
           aria-labelledby="pinned-project-options-menu-0">
        <div class="elsa-py-1">
          <a
            onClick={e => this.onPasteActivityClick(e)}
            href="#"
            class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-leading-5 elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900 focus:elsa-outline-none focus:elsa-bg-gray-100 focus:elsa-text-gray-900"
            role="menuitem">
            {t('ConnectionContextMenu.Paste')}
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
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke="currentColor" fill="none"
             class="elsa-h-8 elsa-w-8">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
        </svg>
      </button>
    );
  }

  renderWorkflowHelpButton() {
    return (
      <span>
        <button type="button"
                onClick={this.showHelpModal}
                class="workflow-settings-button elsa-fixed elsa-top-20 elsa-right-28 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500">
          <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-8 elsa-w-8" fill="none" viewBox="0 0 24 24"
               stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </button>
        <elsa-modal-dialog ref={el => this.helpDialog = el}>
          <div slot="content" class="elsa-p-8">
            <h3 class="elsa-text-lg elsa-font-medium">Actions</h3>
            <dl
              class="elsa-mt-2 elsa-border-t elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
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
        </elsa-modal-dialog>
      </span>
    );
  }

  showHelpModal = async () => {
    await this.helpDialog.show();
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

  private renderPanel() {
    return (
      <elsa-flyout-panel expandButtonPosition={3}>
        <elsa-tab-header tab="general" slot="header">General</elsa-tab-header>
        <elsa-tab-content tab="general" slot="content">
          <elsa-workflow-properties-panel
            workflowDefinition={this.workflowDefinition}
          />
        </elsa-tab-content>
        <elsa-tab-header tab="test" slot="header">Test</elsa-tab-header>
        <elsa-tab-content tab="test" slot="content">
          <elsa-workflow-test-panel
            workflowDefinition={this.workflowDefinition}
            workflowTestActivityId={this.selectedActivityId}
          />
        </elsa-tab-content>        
      </elsa-flyout-panel>
    );
  }
}

injectHistory(ElsaWorkflowDefinitionEditorScreen);
DashboardTunnel.injectProps(ElsaWorkflowDefinitionEditorScreen, ['serverUrl', 'culture', 'monacoLibPath']);
