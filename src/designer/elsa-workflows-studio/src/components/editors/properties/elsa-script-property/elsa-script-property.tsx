import {Component, h, Prop, State} from '@stencil/core';
import {
  ActivityDefinitionProperty,
  ActivityPropertyDescriptor,
  ActivityValidatingContext,
  ConfigureComponentCustomButtonContext,
  ComponentCustomButtonClickContext,
  EventTypes, ActivityModel, IntellisenseContext
} from "../../../../models";
import {createElsaClient, eventBus} from "../../../../services";
import Tunnel from '../../../../data/workflow-editor';
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";

@Component({
  tag: 'elsa-script-property',
  shadow: false,
})
export class ElsaScriptProperty {

  @Prop() activityModel: ActivityModel;
  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
  @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
  @Prop() syntax?: string;
  @Prop({mutable: true}) serverUrl: string;
  @Prop({mutable: true}) workflowDefinitionId: string;
  @State() currentValue?: string;

  monacoEditor: HTMLElsaMonacoElement;
  activityValidatingContext: ActivityValidatingContext = null;
  configureComponentCustomButtonContext: ConfigureComponentCustomButtonContext = null;

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expressions['Literal'];
    await this.configureComponentCustomButton();
    this.validate(this.currentValue);
  }

  async componentDidLoad() {
    const elsaClient = await createElsaClient(this.serverUrl);
    const context: IntellisenseContext = {
      propertyName: this.propertyDescriptor.name,
      activityTypeName: this.activityModel.type
    };
    const libSource = await elsaClient.scriptingApi.getJavaScriptTypeDefinitions(this.workflowDefinitionId, context);
    const libUri = 'defaultLib:lib.es6.d.ts';
    await this.monacoEditor.addJavaScriptLib(libSource, libUri);
  }

  async configureComponentCustomButton() {
    this.configureComponentCustomButtonContext = {
      component: 'elsa-script-property',
      activityType: this.activityModel.type,
      prop: this.propertyDescriptor.name,
      data: null
    };
    await eventBus.emit(EventTypes.ComponentLoadingCustomButton, this, this.configureComponentCustomButtonContext);
  }

  mapSyntaxToLanguage(syntax: string): any {
    switch (syntax) {
      case 'JavaScript':
        return 'javascript';
      case 'Liquid':
        return 'handlebars';
      case 'Literal':
      default:
        return 'plaintext';
    }
  }

  onComponentCustomButtonClick(e: Event) {
    e.preventDefault();
    const componentCustomButtonClickContext: ComponentCustomButtonClickContext = {
      component: 'elsa-script-property',
      activityType: this.activityModel.type,
      prop: this.propertyDescriptor.name,
      params: null
    };
    eventBus.emit(EventTypes.ComponentCustomButtonClick, this, componentCustomButtonClickContext);
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    this.currentValue = e.value;
    this.validate(this.currentValue);
  }

  validate(value: string) {
    this.activityValidatingContext = {
      activityType: this.activityModel.type,
      prop: this.propertyDescriptor.name,
      value: value,
      data: null,
      isValidated: false,
      isValid: false
    };
    eventBus.emit(EventTypes.ActivityPluginValidating, this, this.activityValidatingContext);
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const syntax = this.syntax;
    const monacoLanguage = this.mapSyntaxToLanguage(syntax);
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const value = this.currentValue;

    const renderValidationResult = () => {
      if (this.activityValidatingContext == null || !this.activityValidatingContext.isValidated)
        return;

      const isPositiveResult = this.activityValidatingContext.isValid;
      const color = isPositiveResult ? 'green' : 'red';

      return (
        <div class="elsa-mt-3">
          <p class={`elsa-mt-1 elsa-text-sm elsa-text-${color}-500`}>
            {this.activityValidatingContext.data}
          </p>
        </div>
      )
    }

    const renderComponentCustomButton = () => {
      if (this.configureComponentCustomButtonContext.data == null)
        return;

      const label = this.configureComponentCustomButtonContext.data.label;

      return (
        <div class="elsa-mt-3">
          <a href="#"
             onClick={e => this.onComponentCustomButtonClick(e)}
             class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-gray-300 elsa-text-sm elsa-leading-5 elsa-font-medium elsa-rounded-md elsa-text-gray-700 elsa-bg-white hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-shadow-outline-blue focus:elsa-border-blue-300 active:elsa-bg-gray-100 active:elsa-text-gray-700 elsa-transition elsa-ease-in-out elsa-duration-150">
            {label}
          </a>
        </div>
      )
    }

    return <div>

      <div class="elsa-flex">
        <div class="">
          <label htmlFor={fieldId} class="elsa-block elsa-text-sm elsa-font-medium elsa-text-gray-700">
            {fieldLabel}
          </label>
        </div>
        <div class="elsa-flex-1">
          <div>
            <div class="hidden sm:elsa-block">
              <nav class="elsa-flex elsa-flex-row-reverse" aria-label="Tabs">
                <span
                  class="elsa-bg-blue-100 elsa-text-blue-700 elsa-px-3 elsa-py-2 elsa-font-medium elsa-text-sm elsa-rounded-md">
                  {syntax}
                </span>
              </nav>
            </div>
          </div>
        </div>
      </div>
      <div class="elsa-mt-1">
        <elsa-monaco value={value}
                     language={monacoLanguage}
                     editor-height={this.editorHeight}
                     single-line={this.singleLineMode}
                     onValueChanged={e => this.onMonacoValueChanged(e.detail)}
                     ref={el => this.monacoEditor = el}/>
      </div>
      {fieldHint ? <p class="elsa-mt-2 elsa-text-sm elsa-text-gray-500">{fieldHint}</p> : undefined}
      <input type="hidden" name={fieldName} value={value}/>
      {renderValidationResult()}
      {renderComponentCustomButton()}
    </div>
  }
}

Tunnel.injectProps(ElsaScriptProperty, ['serverUrl', 'workflowDefinitionId']);
