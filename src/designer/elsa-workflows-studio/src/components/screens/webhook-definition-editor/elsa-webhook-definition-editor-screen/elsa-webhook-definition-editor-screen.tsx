import {Component, Event, EventEmitter, h, Host, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import {ActivityDefinition, ActivityDescriptor, ActivityModel, ConnectionDefinition, ConnectionModel, VersionOptions, WorkflowPersistenceBehavior} from "../../../../models";
import {EventTypes, WebhookDefinition, WebhookModel} from "../../../../models/webhook";
import {createElsaClient, SaveWebhookDefinitionRequest} from "../../../../services/elsa-client";
import {pluginManager} from '../../../../services/plugin-manager';
import state from '../../../../utils/store';
import Tunnel, {WebhookEditorState} from '../../../../data/webhook-editor';
//import {ActivityContextMenuState, WorkflowDesignerMode} from "../../../designers/tree/elsa-designer-tree/models";
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";
import {MarkerSeverity} from "monaco-editor";
import {checkBox, FormContext, selectField, SelectOption, textArea, textInput} from "../../../../utils/forms";
//import {registerClickOutside} from "stencil-click-outside";

@Component({
  tag: 'elsa-webhook-definition-editor-screen',
  shadow: false,
})
export class ElsaWebhookDefinitionEditorScreen {

  constructor() {
    pluginManager.initialize();
  }

  @Event() webhookSaved: EventEmitter<WebhookDefinition>;
  @Prop({attribute: 'webhook-definition-id', reflect: true}) webhookDefinitionId: string;
  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;
  @State() webhookDefinition: WebhookDefinition;
  @State() webhookModel: WebhookModel;
  @State() saving: boolean;
  @State() saved: boolean;
  @State() networkError: string;
  formContext: FormContext;
  el: HTMLElement;

  @Method()
  async getServerUrl(): Promise<string> {
    return this.serverUrl;
  }

  @Method()
  async getWebhookDefinitionId(): Promise<string> {
    return this.webhookDefinition.definitionId;
  }

  @Watch('webhookDefinitionId')
  async webhookDefinitionIdChangedHandler(newValue: string) {
    const webhookDefinitionId = newValue;
    let webhookDefinition: WebhookDefinition = ElsaWebhookDefinitionEditorScreen.createWebhookDefinition();
    webhookDefinition.definitionId = webhookDefinitionId;
    const client = createElsaClient(this.serverUrl);

    if (webhookDefinitionId && webhookDefinitionId.length > 0) {
      try {
        webhookDefinition = await client.webhookDefinitionsApi.getByDefinition(webhookDefinitionId);
      } catch {
        console.warn(`The specified webhook definition does not exist. Creating a new one.`)
      }
    }

    this.updateWebhookDefinition(webhookDefinition);
  }

  //@Watch('webhookDefinition')
  //handleWebhookDefinitionChanged(newValue: WebhookDefinition) {
//    this.webhookDefinition = {...newValue};
    //this.formContext = new FormContext(this.webhookDefinition, newValue => this.webhookDefinition = newValue);
  //}  

  @Watch("serverUrl")
  async serverUrlChangedHandler(newValue: string) {
  }

  @Watch("monacoLibPath")
  async monacoLibPathChangedHandler(newValue: string) {
    state.monacoLibPath = newValue;
  }

  //@Listen('webhook-changed')
  //async webhookChangedHandler(event: CustomEvent<WebhookModel>) {
    //const webhookModel = event.detail;
//    await this.saveWebhook(webhookModel);
  //}

  async componentWillLoad() {
    await this.serverUrlChangedHandler(this.serverUrl);
    await this.webhookDefinitionIdChangedHandler(this.webhookDefinitionId);
    await this.monacoLibPathChangedHandler(this.monacoLibPath);
  }

  updateWebhookDefinition(value: WebhookDefinition) {
    this.webhookDefinition = value;
    //this.webhookModel = this.mapWorkflowModel(value);
  }

  async saveWebhook(webhookModel?: WebhookModel) {
    if (!this.serverUrl || this.serverUrl.length == 0)
      return;

      webhookModel = webhookModel || this.webhookModel;

    const client = createElsaClient(this.serverUrl);
    let webhookDefinition = this.webhookDefinition;

    const request: SaveWebhookDefinitionRequest = {
      webhookDefinitionId: webhookDefinition.definitionId || this.webhookDefinitionId,      
      name: webhookDefinition.name,
      path: webhookDefinition.path,
      description: webhookDefinition.description,
      payloadTypeName: webhookDefinition.payloadTypeName,
      isEnabled: webhookDefinition.isEnabled,
    };

    //this.saving = !publish;
    //this.publishing = publish;

    try {
      webhookDefinition = await client.webhookDefinitionsApi.save(request);
      this.webhookDefinition = webhookDefinition;
      this.saving = false;
      setTimeout(() => this.saved = false, 500);
      this.webhookSaved.emit(webhookDefinition);
    } catch (e) {
      console.error(e);
      this.saving = false;
      this.saved = false;
      this.networkError = e.message;
      setTimeout(() => this.networkError = null, 10000);
    }
  }

  async onSaveClicked(e: Event) {
    e.preventDefault();
    `debugger`
    await this.saveWebhook();
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    // Don't try and parse JSON if it contains errors.
    const errorCount = e.markers.filter(x => x.severity == MarkerSeverity.Error).length;

    if (errorCount > 0)
      return;

    const newValue = e.value;
    let data = this.webhookDefinition.variables ? this.webhookDefinition.variables.data || {} : {};

    try {
      data = newValue.indexOf('{') >= 0 ? JSON.parse(newValue) : {};
    } catch (e) {
    } finally {
      this.webhookDefinition = {...this.webhookDefinition, variables: {data: data}};
    }
  }  

  render() {
    const tunnelState: WebhookEditorState = {
      serverUrl: this.serverUrl,
      webhookDefinitionId: this.webhookDefinition.definitionId
    };

    return (
      <Host class="elsa-flex elsa-flex-col elsa-w-full" ref={el => this.el = el}>
        

          <form onSubmit={e => this.onSaveClicked(e)}>
              <div class="elsa-px-8 mb-8">
                <div class="elsa-border-b elsa-border-gray-200">
                </div>
              </div>

              {this.renderWebhookFields()}

              <div class="elsa-pt-5">
                <div class="elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
                  <button type="submit"
                          class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    Save
                  </button>
                </div>
              </div>
            </form>

        
      </Host>
    );
  }

  renderWebhookFields() {
    const webhookDefinition = this.webhookDefinition;
    const formContext = this.formContext;

    return (
      <div class="elsa-flex elsa-px-8">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'name', 'Name', webhookDefinition.name, 'The name of the webhook.', 'webhookName')}
          {textInput(formContext, 'path', 'Path', webhookDefinition.path, 'The path of the webhook.', 'webhookPath')}
          {textArea(formContext, 'description', 'Description', webhookDefinition.description, null, 'webhookDescription')}
          {textInput(formContext, 'payloadTypeName', 'Payload Type Name', webhookDefinition.payloadTypeName, 'The payload type name of the webhook.', 'webhookPayloadTypeName')}
          {checkBox(formContext, 'isEnabled', 'Enabled', webhookDefinition.isEnabled, null)}
        </div>
      </div>
    );
  }

  renderSavingIndicator() {

    const message =
      this.saving ? 'Saving...' : this.saved ? 'Saved'
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

  private static createWebhookDefinition(): WebhookDefinition {
    return {
      definitionId: null,
      //version: 1,
      //activities: [],
    };
  }
}
