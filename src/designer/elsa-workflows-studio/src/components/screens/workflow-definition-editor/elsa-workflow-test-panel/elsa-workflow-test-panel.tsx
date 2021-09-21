import {Component, Prop, h, State, Watch, Host} from '@stencil/core';
import {HubConnection, HubConnectionBuilder} from '@microsoft/signalr';
import * as collection from 'lodash/collection';
import {EventTypes, WorkflowDefinition, WorkflowTestActivityMessage, WorkflowTestUpdateRequest} from "../../../../models";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {createElsaClient, eventBus, WorkflowTestExecuteRequest} from "../../../../services";
import Tunnel from "../../../../data/dashboard";
import {convert} from 'json-to-json-schema';
import {clip} from "../../../../utils/utils";

@Component({
  tag: 'elsa-workflow-test-panel',
  shadow: false
})
export class ElsaWorkflowTestPanel {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() workflowTestActivityId: string;
  @Prop() culture: string;
  @Prop() serverUrl: string;
  @State() hubConnection: HubConnection;
  @State() workflowTestActivityMessages: Array<WorkflowTestActivityMessage> = [];

  i18next: i18n;
  signalRConnectionId: string;  
  message: WorkflowTestActivityMessage;

  @Watch('workflowDefinition')
  async workflowDefinitionChangedHandler(newWorkflow: WorkflowDefinition, oldWorkflow: WorkflowDefinition) {
  }

  @Watch('workflowTestActivityId')
  async workflowTestActivityMessageChangedHandler(newMessage: string, oldMessage: string) {
    const message = this.workflowTestActivityMessages.find(x => x.activityId == newMessage);
    this.message = !!message ? message : null;
  }  

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
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
    this.message = null;
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
    debugger
    const value = this.message.data["Body"];
    const request: WorkflowTestUpdateRequest = {
      activityId: this.message.activityId,
      jsonSchema: JSON.stringify(convert(value), null, 4)
    };

    eventBus.emit(EventTypes.ActivityJsonSchemaUpdated, this, request);
  }

  render() {

    const t = (x, params?) => this.i18next.t(x, params);

    return (
      <Host>
        <dl class="elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <button type="button"
                    onClick={() => this.onExecuteWorkflowClick()}
                    class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
              {t('ExecuteWorkflow')}
            </button>
          </div>
        </dl>
        {this.renderActivityTestMessage()}
      </Host>
    );
  }


  renderActivityTestMessage() {

    const {message} = this;

    if (message == undefined || !message)
      return    
      
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
      <dl class="elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
        <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
          <dt class="elsa-text-gray-500">
            <pre onClick={e => clip(e.currentTarget)}>{this.serverUrl + '/workflows' + message.path + '?correlation=' + message.correlationId}</pre>
          </dt>
        </div>
        <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
          <dt class="elsa-text-gray-500">{t('Status')}</dt>
          <dd class="elsa-text-gray-900">{message.status}</dd>
        </div>
        {collection.map(filteredData, (v, k) => (
        <div>
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <dt class="elsa-text-gray-500">{k}</dt>
            <dd class="elsa-mt-1 elsa-text-sm elsa-text-gray-900 elsa-mb-2 elsa-overflow-x-auto">{v}</dd>
          </div>
          {hasBody ? 
            <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
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

    const {message} = this;

    if (message == undefined || !message)
      return    
      
    const t = (x, params?) => this.i18next.t(x, params);

    if (!message.error)
      return;

    return (
      <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
        <dt class="elsa-text-gray-500">{t('Error')}</dt>
        <dd class="elsa-mt-1 elsa-text-sm elsa-text-gray-900 elsa-mb-2 elsa-overflow-x-auto">{message.error}</dd>
      </div>
    );
  }  

  createClient() {
    return createElsaClient(this.serverUrl);
  }
}

Tunnel.injectProps(ElsaWorkflowTestPanel, ['serverUrl', 'culture']);