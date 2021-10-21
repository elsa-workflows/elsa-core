import {Component, Prop, h, State, Watch, Host} from '@stencil/core';
import {HubConnection, HubConnectionBuilder} from '@microsoft/signalr';
import {EventTypes, WorkflowDefinition, WorkflowTestActivityMessage} from "../../../../models";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {createElsaClient, eventBus, WorkflowTestExecuteRequest} from "../../../../services";
import Tunnel from "../../../../data/dashboard";
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
  @State() workflowStarted: boolean = false;

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
      message = message as WorkflowTestActivityMessage;
      message.data = JSON.parse(message.data);
      this.workflowTestActivityMessages = this.workflowTestActivityMessages.filter(x => x.activityId !== message.activityId);
      this.workflowTestActivityMessages = [...this.workflowTestActivityMessages, message];      
      eventBus.emit(EventTypes.TestActivityMessageReceived, this, message);

      if (message.workflowStatus == 'Executed') {
        this.workflowStarted = false;
      }

      if (!this.message && message.activityId == this.workflowTestActivityId){
        this.message = message;
      }
    });

    this.hubConnection.start()
      .then(() => this.hubConnection.invoke("Connecting"))
      .catch((err) => console.log('error while establishing SignalR connection: ' + err));
  }

  async onExecuteWorkflowClick() {
    this.message = null;
    this.workflowStarted = true;
    this.workflowTestActivityMessages = [];
    eventBus.emit(EventTypes.TestActivityMessageReceived, this, null);

    const request: WorkflowTestExecuteRequest = {
      workflowDefinitionId: this.workflowDefinition.definitionId,
      version: this.workflowDefinition.version,
      signalRConnectionId: this.signalRConnectionId
    };

    const client = await createElsaClient(this.serverUrl);
    await client.workflowTestApi.execute(request);
  }

  async onStopWorkflowClick() {
    const message = this.workflowTestActivityMessages.last();
    if (!!message) {
      const client = await createElsaClient(this.serverUrl);
      await client.workflowInstancesApi.delete(message.workflowInstanceId);
    }

    this.message = null;
    this.workflowStarted = false;
    this.workflowTestActivityMessages = [];
    eventBus.emit(EventTypes.TestActivityMessageReceived, this, null);
  }

  render() {

    const t = (x, params?) => this.i18next.t(x, params);

    const renderActivityTestMessage = () => {

      const {message} = this;

      if (message == undefined || !message)
        return

      const workflowStatus = this.workflowTestActivityMessages.last().workflowStatus;

      const t = (x, params?) => this.i18next.t(x, params);

      const renderEndpointUrl = () => {
        if (!message.activityData || !message.activityData["Path"]) return undefined;

        const endpointUrl = this.serverUrl + '/workflows' + message.activityData["Path"].value + '?correlation=' + message.correlationId;

        return (
          <div>
              <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                <dt class="elsa-text-gray-500">
                  <span class="elsa-mr-1">{t('EntryEndpoint')}</span>
                  <elsa-copy-button value={endpointUrl} />
                </dt>
              </div>
              <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
                <dt class="elsa-text-gray-900">
                  <span class="elsa-break-all font-mono" onClick={e => clip(e.currentTarget)}>{endpointUrl}</span>
                </dt>
              </div>
            </div>
        );
      };      
  
      return (      
        <dl class="elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            <dt class="elsa-text-gray-500">{t('Status')}</dt>
            <dd class="elsa-text-gray-900">{workflowStatus}</dd>
          </div>            
          {renderEndpointUrl()}   
        </dl>
      );
    }    

    return (
      <Host>
        <dl class="elsa-border-b elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-py-3 elsa-flex elsa-justify-between elsa-text-sm elsa-font-medium">
            {!this.workflowStarted ?
              <button type="button"
                      onClick={() => this.onExecuteWorkflowClick()}
                      class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                {t('ExecuteWorkflow')}
              </button> 
              :
              <button type="button"
                      onClick={() => this.onStopWorkflowClick()}
                      class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-red-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-red-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-red-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                {t('StopWorkflow')}
              </button>             
            }
          </div>
        </dl>
        {renderActivityTestMessage()}
      </Host>
    );
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }
}

Tunnel.injectProps(ElsaWorkflowTestPanel, ['serverUrl', 'culture']);