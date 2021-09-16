import {Component, Prop, h, Method, State, Watch, Host} from '@stencil/core';
import {HubConnection, HubConnectionBuilder} from '@microsoft/signalr';
import * as collection from 'lodash/collection';
import {EventTypes, WorkflowDefinition, WorkflowDefinitionSummary, WorkflowTestActivityMessage, WorkflowTestUpdateRequest} from "../../../../models";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {createElsaClient, eventBus, WorkflowTestExecuteRequest} from "../../../../services";
import Tunnel from "../../../../data/dashboard";
import {convert} from 'json-to-json-schema';

interface Tab {
  id: string;
  text: string;
  view: () => any;
}

@Component({
  tag: 'elsa-workflow-test-panel',
  shadow: false
})
export class ElsaWorkflowTestPanel {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() culture: string;
  @Prop() serverUrl: string;
  @State() publishedVersion: number;
  @State() hubConnection: HubConnection;
  @State() workflowTestActivityMessages: Array<WorkflowTestActivityMessage> = [];

  i18next: i18n;
  signalRConnectionId: string;
  el: HTMLElement;
  tabs: Array<Tab> = [];
  testActivity: WorkflowTestActivityMessage;

  @Method()
  async selectTestActivity(activityId?: string) {
    const message = this.workflowTestActivityMessages.find(x => x.activityId == activityId)
    const messageInternal = !!message ? message : null;
    this.selectTestActivityMessageInternal(messageInternal);
  }

  @Watch('workflowDefinition')
  async workflowDefinitionChangedHandler(newWorkflow: WorkflowDefinition, oldWorkflow: WorkflowDefinition) {
    if (newWorkflow.version !== oldWorkflow.version || newWorkflow.isPublished !== oldWorkflow.isPublished || newWorkflow.isLatest !== oldWorkflow.isLatest)
      await this.loadPublishedVersion();
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    await this.loadPublishedVersion();
    this.connectMessageHub();
  }

  private connectMessageHub(): void {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.serverUrl + "/hubs/workflowTest")
      .build();

    this.hubConnection.on('Connected', (message) => {
      this.signalRConnectionId = message;
    });

    this.hubConnection.on('DispatchMessage', (message) => {
      message.data = JSON.parse(message.data);
      this.workflowTestActivityMessages = this.workflowTestActivityMessages.filter(x => x.activityId !== message.activityId);
      this.workflowTestActivityMessages = [...this.workflowTestActivityMessages, message];      
      eventBus.emit(EventTypes.TestActivityMessageReceived, this, message);
    });

    this.hubConnection.start()
      .then(() => this.hubConnection.invoke("Connecting"))
      .catch((err) => console.log('error while establishing SignalR connection: ' + err));
  }

  async onExecuteWorkflowClick() {
    this.workflowTestActivityMessages = [];
    eventBus.emit(EventTypes.TestActivityMessageReceived, this, null);
    const elsaClient = this.createClient();

    const request: WorkflowTestExecuteRequest = {
      workflowDefinitionId: this.workflowDefinition.definitionId,
      version: this.workflowDefinition.version,
      signalRConnectionId: this.signalRConnectionId
    };

    await elsaClient.workflowTestApi.execute(request);
  }

  async onUseAsSchemaClick() {    
    const value = this.testActivity.data["Inbound Request"];
    const request: WorkflowTestUpdateRequest = {
      activityId: this.testActivity.activityId,
      jsonSchema: JSON.stringify(convert(value), null, 4)
    };

    eventBus.emit(EventTypes.ActivityJsonSchemaUpdated, this, request);
  }

  selectTestActivityMessageInternal(message?: WorkflowTestActivityMessage) {
    this.testActivity = message;
  }

  render() {

    const t = (x, params?) => this.i18next.t(x, params);

    return (
      <Host>
      <div class="elsa-h-full elsa-flex elsa-flex-col elsa-py-6 elsa-bg-white elsa-shadow-xl elsa-overflow-y-scroll elsa-bg-white">
        <div class="elsa-h-full">
          <div class="elsa-p-6">
            <div class="elsa-px-4 elsa-py-3 elsa-bg-gray-50 elsa-text-left sm:px-6">
              <button type="button"
                      onClick={() => this.onExecuteWorkflowClick()}
                      class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                {t('ExecuteWorkflow')}
              </button>
            </div>
            {this.renderActivityTestMessage()}            
          </div>
        </div>
      </div>
      </Host>
    );
  }


  renderActivityTestMessage() {

    const {testActivity} = this;

    if (testActivity == undefined)
      return    
      
    const t = (x, params?) => this.i18next.t(x, params);
    const filteredData = {};
    const wellKnownDataKeys = {State: true, Input: null, Outcomes: true, Exception: true};
    let dataKey = null;

    for (const key in testActivity.data) {
      if (!testActivity.data.hasOwnProperty(key))
        continue;

      if (!!wellKnownDataKeys[key])
        continue;

      const value = testActivity.data[key];

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

    const isInboundRequest = dataKey === "Inbound Request";

    return (
      <dl
        class="elsa-mt-2 elsa-border-t elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
        <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
          <dt class="elsa-text-gray-500">{'Correlation Id'}</dt>
          <dd class="elsa-text-gray-900">{testActivity.correlationId}</dd>
        </div>                      
        <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
          <dt class="elsa-text-gray-500">{t('Status')}</dt>
          <dd class="elsa-text-gray-900 elsa-break-all">{testActivity.status || '-'}</dd>
        </div>
        {collection.map(filteredData, (v, k) => (
          <div>
            <div class="sm:elsa-col-span-2">
              <dt class="elsa-text-sm elsa-font-medium elsa-text-gray-500 elsa-capitalize">{k}</dt>
              <dd class="elsa-mt-1 elsa-text-sm elsa-text-gray-900 elsa-mb-2 elsa-overflow-x-auto">{v}</dd>
            </div>
            {isInboundRequest ? 
              <div class="elsa-px-4 elsa-py-3 elsa-bg-gray-50 elsa-text-left sm:px-6">
                <button type="button"
                        onClick={() => this.onUseAsSchemaClick()}
                        class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                  {t('UseAsSchema')}
                </button>
              </div>
              :
              null
            }
          </div>
        ))}
        {this.renderActivityTestError()}        
      </dl>
    );
  }

  renderActivityTestError() {

    const {testActivity} = this;

    if (testActivity == undefined)
      return    
      
    const t = (x, params?) => this.i18next.t(x, params);

    if (!testActivity.error)
      return;

    return (
      <div class="sm:elsa-col-span-2">
        <dt class="elsa-text-sm elsa-font-medium elsa-text-gray-500 elsa-capitalize">{t('Error')}</dt>
        <dd class="elsa-mt-1 elsa-text-sm elsa-text-gray-900 elsa-mb-2 elsa-overflow-x-auto">{testActivity.error}</dd>
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

    if (publishedDefinition) {
      this.publishedVersion = publishedDefinition.version;
    }
  }  
}

Tunnel.injectProps(ElsaWorkflowTestPanel, ['serverUrl', 'culture']);