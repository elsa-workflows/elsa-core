import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";
import {createElsaClient} from "../../../../services/elsa-client";
import Tunnel from '../../../../data/workflow-editor';
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";

@Component({
  tag: 'elsa-script-property',
  shadow: false,
})
export class ElsaScriptProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
  @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
  @Prop({attribute: 'context', reflect: true}) context?: string;
  @Prop() syntax?: string;
  @Prop({mutable: true}) serverUrl: string;
  @Prop({mutable: true}) workflowDefinitionId: string;
  @State() currentValue?: string
  monacoEditor: HTMLElsaMonacoElement;

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expressions['Literal'];
  }

  async componentDidLoad() {
    const elsaClient = createElsaClient(this.serverUrl);
    const libSource = await elsaClient.scriptingApi.getJavaScriptTypeDefinitions(this.workflowDefinitionId, this.context);
    const libUri = 'defaultLib:lib.es6.d.ts';
    await this.monacoEditor.addJavaScriptLib(libSource, libUri);
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

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    this.currentValue = e.value;
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
                <span class="elsa-bg-blue-100 elsa-text-blue-700 elsa-px-3 elsa-py-2 elsa-font-medium elsa-text-sm elsa-rounded-md">
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
    </div>
  }
}

Tunnel.injectProps(ElsaScriptProperty, ['serverUrl', 'workflowDefinitionId']);
