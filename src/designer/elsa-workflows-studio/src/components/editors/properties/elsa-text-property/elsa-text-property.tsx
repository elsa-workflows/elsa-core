import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";
import {createElsaClient} from "../../../../services/elsa-client";
import Tunnel from '../../../data/workflow-editor';
import {MonacoValueChangedArgs} from "../../monaco/elsa-monaco/elsa-monaco";

@Component({
  tag: 'elsa-text-property',
  styleUrl: 'elsa-text-property.css',
  shadow: false,
})
export class ElsaTextProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
  @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
  @Prop({attribute: 'context', reflect: true}) context?: string;
  @Prop({mutable: true}) serverUrl: string;
  @Prop({mutable: true}) workflowDefinitionId: string;
  @State() selectedSyntax?: string;
  @State() currentValue?: string
  monacoEditor: HTMLElsaMonacoElement;

  async componentWillLoad() {
    this.selectedSyntax = this.propertyModel.syntax;
    this.currentValue = this.propertyModel.expression;
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

  onSyntaxListChange(e: Event) {
    this.selectedSyntax = (e.currentTarget as HTMLSelectElement).value;
  }

  onSyntaxSelect(e: Event, syntax: string) {
    e.preventDefault()
    this.selectedSyntax = syntax;
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    this.currentValue = e.value;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const options = propertyDescriptor.options || {};
    const syntaxes = options.syntaxes || ['Literal', 'JavaScript', 'Liquid'];
    const selectedSyntax = this.selectedSyntax ?? (syntaxes.length > 0 ? syntaxes[0] : '');
    const monacoLanguage = this.mapSyntaxToLanguage(selectedSyntax);
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const syntaxFieldName = `${fieldName}Syntax`;
    const value = this.currentValue;
    const reversedSyntaxes = [...syntaxes].reverse();

    return <div>

      <div class="flex">
        <div class="">
          <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
            {fieldLabel}
          </label>
        </div>
        <div class="flex-1">
          <div class="">
            <div class="sm:hidden">
              <label htmlFor="tabs" class="sr-only">Select a tab</label>
              <select id="tabs" name="tabs" onChange={e => this.onSyntaxListChange(e)} class="block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md">
                {reversedSyntaxes.map(syntax => {
                  return <option selected={syntax == selectedSyntax}>{syntax}</option>;
                })}
              </select>
            </div>
            <div class="hidden sm:block">
              <nav class="flex flex-row-reverse" aria-label="Tabs">
                {reversedSyntaxes.map(syntax => {
                  const isSelected = syntax == selectedSyntax;
                  const className = isSelected ? 'bg-blue-100 text-blue-700' : 'text-gray-500 hover:text-gray-700';

                  return <a href="#" onClick={e => this.onSyntaxSelect(e, syntax)} class={`${className} px-3 py-2 font-medium text-sm rounded-md`}>
                    {syntax}
                  </a>;
                })}
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
      <input type="hidden" name={syntaxFieldName} value={selectedSyntax}/>
    </div>
  }
}

Tunnel.injectProps(ElsaTextProperty, ['serverUrl', 'workflowDefinitionId']);
