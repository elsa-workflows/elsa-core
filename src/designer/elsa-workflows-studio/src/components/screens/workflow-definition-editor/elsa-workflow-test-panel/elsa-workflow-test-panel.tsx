import { Component, h, Host, Prop, State, Watch } from '@stencil/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { EventTypes, WorkflowDefinition, WorkflowStatus, WorkflowTestActivityMessage } from "../../../../models";
import { i18n } from "i18next";
import { loadTranslations } from "../../../i18n/i18n-loader";
import { resources } from "./localizations";
import {
  createElsaClient,
  eventBus,
  WorkflowTestExecuteRequest,
  WorkflowTestRestartFromActivityRequest
} from "../../../../services";
import Tunnel from "../../../../data/dashboard";
import { clip } from "../../../../utils/utils";

@Component({
  tag: 'elsa-workflow-test-panel',
  shadow: false
})
export class ElsaWorkflowTestPanel {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() workflowTestActivityId: string;
  @Prop() culture: string;
  @Prop() serverUrl: string;
  @Prop() selectedActivityId?: string;
  @State() hubConnection: HubConnection;
  @State() workflowTestActivityMessages: Array<WorkflowTestActivityMessage> = [];
  @State() workflowStarted: boolean = false;

  i18next: i18n;
  signalRConnectionId: string;
  message: WorkflowTestActivityMessage;
  confirmDialog: HTMLElsaConfirmDialogElement;

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
    const builder = new HubConnectionBuilder()
      .withUrl(this.serverUrl + "/hubs/workflowTest");

    eventBus.emit(EventTypes.HubConnectionCreated, this, builder);
    this.hubConnection = builder.build();

    this.hubConnection.on('Connected', (message) => {
      this.signalRConnectionId = message;
      eventBus.emit(EventTypes.HubConnectionConnected, this, { hubConnection: this.hubConnection, connectionId: message });
    });

    this.hubConnection.onclose((e) => {
      eventBus.emit(EventTypes.HubConnectionClosed, this, { hubConnection: this.hubConnection, error: e});
    })

    this.hubConnection.on('DispatchMessage', async (message) => {
      message = message as WorkflowTestActivityMessage;
      message.data = JSON.parse(message.data);
      this.workflowTestActivityMessages = this.workflowTestActivityMessages.filter(x => x.activityId !== message.activityId);
      this.workflowTestActivityMessages = [...this.workflowTestActivityMessages, message];
      await eventBus.emit(EventTypes.TestActivityMessageReceived, this, message);

      if (message.workflowStatus === 'Executed' || message.workflowStatus === 'Failed') {
        this.workflowStarted = false;
      }

      if (message.workflowStatus === 'Suspended') {
        this.workflowStarted = true;
      }

      if (!this.message) {
        this.message = message;
      }
    });

    this.hubConnection.start()
      .then(() => {
        this.hubConnection.invoke("Connecting");
        eventBus.emit(EventTypes.HubConnectionStarted, this, this.hubConnection);
      })
      .catch((err) => {
        console.log('Failed to establish a SignalR connection.')
        eventBus.emit(EventTypes.HubConnectionFailed, this, { hubConnection: this.hubConnection, err: err });
      });
  }

  connectedCallback() {
    eventBus.on(EventTypes.WorkflowRestarted, this.onRestartWorkflow);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WorkflowRestarted, this.onRestartWorkflow);
  }

  async onExecuteWorkflowClick() {
    await eventBus.emit(EventTypes.WorkflowExecuted, this);

    this.message = null;
    this.workflowStarted = true;
    this.workflowTestActivityMessages = [];
    await eventBus.emit(EventTypes.TestActivityMessageReceived, this, null);

    const request: WorkflowTestExecuteRequest = {
      workflowDefinitionId: this.workflowDefinition.definitionId,
      version: this.workflowDefinition.version,
      signalRConnectionId: this.signalRConnectionId,
      startActivityId: this.selectedActivityId
    };

    const client = await createElsaClient(this.serverUrl);
    const response = await client.workflowTestApi.execute(request);

    if (!response.isSuccess && response.isAnotherInstanceRunning) {
      this.workflowStarted = false;

      const t = x => this.i18next.t(x);
      const result = await this.confirmDialog.show(t('RestartInstanceConfirmationModel.Title'), t('RestartInstanceConfirmationModel.Message'));

      if (!!result) {
        const runningInstances = await client.workflowInstancesApi.list(null, null, this.workflowDefinition.definitionId, WorkflowStatus.Suspended);

        for (const instance of runningInstances.items) {
          await client.workflowTestApi.stop({ workflowInstanceId: instance.id });
          await client.workflowInstancesApi.delete(instance.id);
        }

        await this.onExecuteWorkflowClick();
      }
    }
  }

  onRestartWorkflow = async (selectedActivityId: string) => {
    const message = this.message?.activityId === selectedActivityId ? this.message : this.workflowTestActivityMessages.find(x => x.activityId === selectedActivityId);

    const request: WorkflowTestRestartFromActivityRequest = {
      workflowDefinitionId: this.workflowDefinition.definitionId,
      version: this.workflowDefinition.version,
      signalRConnectionId: this.signalRConnectionId,
      activityId: message.activityId,
      lastWorkflowInstanceId: message.workflowInstanceId
    };

    this.workflowStarted = true;

    const client = await createElsaClient(this.serverUrl);
    await client.workflowTestApi.restartFromActivity(request);
  }

  async onStopWorkflowClick() {
    const message = this.workflowTestActivityMessages.last();
    if (!!message) {
      const client = await createElsaClient(this.serverUrl);
      await client.workflowInstancesApi.delete(message.workflowInstanceId);
      await client.workflowTestApi.stop({ workflowInstanceId: message.workflowInstanceId });
    }

    this.message = null;
    this.workflowStarted = false;
    this.workflowTestActivityMessages = [];
    await eventBus.emit(EventTypes.TestActivityMessageReceived, this, null);
  }

  render() {

    const t = (x, params?) => this.i18next.t(x, params);

    const renderActivityTestMessage = () => {

      const { message } = this;

      if (message == undefined || !message)
        return

      const workflowStatus = this.workflowTestActivityMessages.last().workflowStatus;

      const renderEndpointUrl = () => {
        if (!message.activityData || !message.activityData["Path"]) return undefined;

        const endpointUrl = this.serverUrl + '/workflows' + message.activityData["Path"] + '?correlation=' + message.correlationId;

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
        <elsa-confirm-dialog ref={el => this.confirmDialog = el} culture={this.culture} />
      </Host>
    );
  }
}

Tunnel.injectProps(ElsaWorkflowTestPanel, ['serverUrl', 'culture']);
