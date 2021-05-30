import {Component, Event, Host, Prop, State, Watch, h} from '@stencil/core';
import {eventBus} from "../../../../services/event-bus";
import {Map} from "../../../../utils/utils";
//import {EventTypes, Variables, WorkflowContextFidelity, WorkflowContextOptions, WorkflowDefinition, WorkflowPersistenceBehavior} from "../../../../models";
import {EventTypes, WebhookDefinition} from "../../../../models/webhook";
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";
import {MarkerSeverity} from "monaco-editor";
import {checkBox, FormContext, selectField, SelectOption, textArea, textInput} from "../../../../utils/forms";

interface VariableDefinition {
  name?: string;
  value?: string
}

@Component({
  tag: 'elsa-webhook-settings-modal',
  shadow: false,
})
export class ElsaWebhookDefinitionSettingsModal {

  @Prop() webhookDefinition: WebhookDefinition;
  @State() webhookDefinitionInternal: WebhookDefinition;
  @State() newVariable: VariableDefinition = {};
  //dialog: HTMLElsaModalDialogElement;
  monacoEditor: HTMLElsaMonacoElement;
  formContext: FormContext;

  @Watch('webhookDefinition')
  handleWebhookDefinitionChanged(newValue: WebhookDefinition) {
    this.webhookDefinitionInternal = {...newValue};
    this.formContext = new FormContext(this.webhookDefinitionInternal, newValue => this.webhookDefinitionInternal = newValue);
  }

  componentWillLoad() {
    this.handleWebhookDefinitionChanged(this.webhookDefinition);
  }

  componentDidLoad() {
    //eventBus.on(EventTypes.ShowWorkflowSettings, async () => await this.dialog.show(true));
  }

  async onCancelClick() {
    //await this.dialog.hide(true);
  }

  async onSubmit(e: Event) {
    e.preventDefault();
    //await this.dialog.hide(true);
    setTimeout(() => eventBus.emit(EventTypes.UpdateWebhook, this, this.webhookDefinitionInternal), 250)
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    // Don't try and parse JSON if it contains errors.
    const errorCount = e.markers.filter(x => x.severity == MarkerSeverity.Error).length;

    if (errorCount > 0)
      return;

    const newValue = e.value;
    let data = this.webhookDefinitionInternal.variables ? this.webhookDefinitionInternal.variables.data || {} : {};

    try {
      data = newValue.indexOf('{') >= 0 ? JSON.parse(newValue) : {};
    } catch (e) {
    } finally {
      this.webhookDefinitionInternal = {...this.webhookDefinitionInternal, variables: {data: data}};
    }
  }

  render() {

    return (
      <Host>

          <div slot="content" class="elsa-py-8 elsa-pb-0">

            <form onSubmit={e => this.onSubmit(e)}>

              {this.renderInputFields()}

              <div class="elsa-pt-5">
                <div class="elsa-bg-gray-50 elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
                  <button type="submit"
                          class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    Save
                  </button>
                  <button type="button"
                          onClick={() => this.onCancelClick()}
                          class="elsa-mt-3 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-white elsa-text-base elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-mt-0 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    Cancel
                  </button>
                </div>
              </div>
            </form>
          </div>

          <div slot="buttons"/>

      </Host>
    );
  }



  renderInputFields() {
    const webhookDefinition = this.webhookDefinitionInternal;
    const formContext = this.formContext;

    return (
      <div class="elsa-flex elsa-px-8">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'name', 'Name', webhookDefinition.name, 'The technical name of the webhook.', 'webhookName')}
          {textInput(formContext, 'path', 'Path', webhookDefinition.path, 'A webhook path.', 'webhookPath')}
          {textArea(formContext, 'description', 'Description', webhookDefinition.description, null, 'webhookDescription')}
          {textInput(formContext, 'payloadTypeName', 'Payload Type Name', webhookDefinition.payloadTypeName, 'PayloadTypeName.', 'payloadTypeName')}
          {checkBox(formContext, 'isEnabled', 'Enabled', webhookDefinition.isEnabled, 'Singleton workflows will only have one active instance executing at a time.')}
        </div>
      </div>
    );
  }

}
