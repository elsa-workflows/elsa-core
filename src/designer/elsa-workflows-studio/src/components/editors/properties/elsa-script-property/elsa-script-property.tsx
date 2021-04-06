import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";
import {createElsaClient} from "../../../../services/elsa-client";
import Tunnel from '../../../../data/workflow-editor';
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";

@Component({
  tag: 'elsa-script-property',
  styleUrl: 'elsa-script-property.css',
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
    const options = propertyDescriptor.options || {};
    const syntax = this.syntax;
    const monacoLanguage = this.mapSyntaxToLanguage(syntax);
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const property = this.propertyModel;
    const value = this.currentValue;

    return <div>

      <div class="flex">
        <div class="">
          <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
            {fieldLabel}
          </label>
        </div>
        <div class="flex-1">
          <div>
            <div class="hidden sm:block">
              <nav class="flex flex-row-reverse" aria-label="Tabs">
                <span class="bg-blue-100 text-blue-700 px-3 py-2 font-medium text-sm rounded-md">
                  {syntax}
                </span>
              </nav>
            </div>
          </div>
        </div>
      </div>
      <div class="mt-1">
        <elsa-monaco value={value}
                     language={monacoLanguage}
                     editor-height={this.editorHeight}
                     single-line={this.singleLineMode}
                     onValueChanged={e => this.onMonacoValueChanged(e.detail)}
                     ref={el => this.monacoEditor = el}/>
      </div>
      {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
      <input type="hidden" name={fieldName} value={value}/>
    </div>
  }
}

Tunnel.injectProps(ElsaScriptProperty, ['serverUrl', 'workflowDefinitionId']);
