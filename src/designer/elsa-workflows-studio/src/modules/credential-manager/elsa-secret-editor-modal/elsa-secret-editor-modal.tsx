import { Component, h, Host, Prop, State } from "@stencil/core";
import { i18n } from "i18next";
import { initializeMonacoWorker } from "../../../components/controls/elsa-monaco/elsa-monaco-utils";
import { resources } from "../../../components/controls/elsa-pager/localizations";
import { loadTranslations } from "../../../components/i18n/i18n-loader";
import { eventBus, propertyDisplayManager } from "../../../services";
import { FormContext, textInput } from "../../../utils/forms";
import state from "../utils/secret.store";
import { SecretDescriptor, SecretEditorRenderProps, SecretModel, SecretPropertyDescriptor } from "../models/secret.model";
import { SecretEventTypes } from "../models/secret.events";

@Component({
    tag: 'elsa-secret-editor-modal',
    shadow: false
})
export class ElsaSecretEditorModal {
  @Prop() culture: string;
  @State() secretModel: SecretModel;
  @State() secretDescriptor: SecretDescriptor;
  @State() renderProps: SecretEditorRenderProps = {};
  i18next: i18n;
  dialog: HTMLElsaModalDialogElement;
  formContext: FormContext;

  timestamp: Date = new Date();

  connectedCallback() {
    eventBus.on(SecretEventTypes.SecretsEditor.Show, this.onShowSecretEditor);
  }

  disconnectedCallback() {
    eventBus.detach(SecretEventTypes.SecretsEditor.Show, this.onShowSecretEditor);
  }

  async componentWillLoad() {
    await initializeMonacoWorker('build/assets/js/monaco-editor/min');
    this.i18next = await loadTranslations(this.culture, resources);
  }

  t = (key: string) => this.i18next.t(key);

  updateSecret(formData: FormData) {
    const secret = this.secretModel;
    const secretDescriptor = this.secretDescriptor;
    const inputProperties: Array<SecretPropertyDescriptor> = secretDescriptor.inputProperties;

    for (const property of inputProperties)
      propertyDisplayManager.update(secret, property, formData);
  }

  async componentWillRender() {
    const secretDescriptor: SecretDescriptor = this.secretDescriptor || {
      displayName: '',
      type: '',
      outcomes: [],
      category: '',
      browsable: false,
      inputProperties: [],
      description: '',
      customAttributes: {}
    };

    const defaultProperties = secretDescriptor.inputProperties.filter(x => !x.category || x.category.length == 0);

    const secretModel: SecretModel = this.secretModel || {
      type: '',
      id: '',
      properties: [],
    };

    const t = this.t;

    this.renderProps = {
      secretDescriptor: secretDescriptor,
      secretModel,
      defaultProperties,
    };
  }

  async onCancelClick() {
    await this.hide(true);
  }

  onSubmit = async (e: Event) => {
    e.preventDefault();
    const form: any = e.target;
    const formData = new FormData(form);
    this.updateSecret(formData);
    await eventBus.emit(SecretEventTypes.UpdateSecret, this, this.secretModel);
    await this.hide(true);

    await eventBus.emit(SecretEventTypes.SecretUpdated, this, this.secretModel);
  };

  onShowSecretEditor = async (secret: SecretModel, animate: boolean) => {
    this.secretModel = JSON.parse(JSON.stringify(secret));
    this.secretDescriptor = state.secretsDescriptors.find(x => x.type == secret.type);
    this.formContext = new FormContext(this.secretModel, newValue => this.secretModel = newValue);

    this.timestamp = new Date();
    this.renderProps = {};
    await this.show(animate);
  };

  show = async (animate: boolean) => await this.dialog.show(animate);
  hide = async (animate: boolean) => await this.dialog.hide(animate);

  onKeyDown = async (event: KeyboardEvent) => {
    if (event.ctrlKey && event.key === 'Enter') {
      (this.dialog.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    }
  }

  render() {
    const renderProps = this.renderProps;
    const secretDescriptor: SecretDescriptor = renderProps.secretDescriptor;
    const secretModel = this.secretModel;
    const t = this.t;

    return (
      <Host class="elsa-block">
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="elsa-py-8 elsa-pb-0">
            <form onSubmit={e => this.onSubmit(e)} key={this.timestamp.getTime().toString()} onKeyDown={this.onKeyDown}
                  class='activity-editor-form'>
              <div class="elsa-flex elsa-px-8">
                <div class="elsa-space-y-8 elsa-divide-y elsa-divide-gray-200 elsa-w-full">
                  <div>
                    <div>
                      <h3 class="elsa-text-lg elsa-leading-6 elsa-font-medium elsa-text-gray-900">
                        {secretDescriptor.type}
                      </h3>
                      <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
                        {secretDescriptor.description}
                      </p>
                    </div>

                    <div class="elsa-mt-8">
                      {this.renderProperties(secretModel)}

                    </div>
                  </div>

                </div>
              </div>
              <div class="elsa-pt-5">
                <div class="elsa-bg-gray-50 elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
                  <button type="submit"
                          class="elsa-ml-3 elsa-inline-flex elsa-justify-center elsa-py-2 elsa-px-4 elsa-border elsa-border-transparent elsa-shadow-sm elsa-text-sm elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-blue-600 hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500">
                    {'Save'}
                  </button>
                  <button type="button"
                          onClick={() => this.onCancelClick()}
                          class="elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-white elsa-text-base elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-mt-0 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    {'Cancel'}
                  </button>
                </div>
              </div>
            </form>
          </div>

          <div slot="buttons"/>
        </elsa-modal-dialog>
      </Host>
    );
  }

  renderProperties(secretModel: SecretModel) {
    const propertyDescriptors: Array<SecretPropertyDescriptor> = this.renderProps.defaultProperties;
    const formContext = this.formContext;

    if (propertyDescriptors.length == 0)
      return undefined;

    const key = `secret-settings:${secretModel.id}`;
    const t = this.t;

    return (
      <div>
        <div class="elsa-w-full">
          {textInput(formContext, 'name', 'Name', secretModel.name, 'Secret\'s name', 'secretName')}
          {textInput(formContext, 'type', 'Type', secretModel.displayName, 'Secret\'s type', 'secretDisplayName', true)}
        </div>
        <div class="elsa-mt-6">
          <div key={key} class={`elsa-grid elsa-grid-cols-1 elsa-gap-y-6 elsa-gap-x-4 sm:elsa-grid-cols-6`}>
            {propertyDescriptors.map(property => this.renderPropertyEditor(secretModel, property))}
          </div>
        </div>
      </div>
    );
  }

  renderPropertyEditor(secret: SecretModel, property: SecretPropertyDescriptor) {
    const key = `secret-property-input:${secret.id}:${property.name}`;
    const display = propertyDisplayManager.display(secret, property);
    const id = `${property.name}Control`;

    return <elsa-control key={key} id={id} class="sm:elsa-col-span-6" content={display}/>;
  }
}
