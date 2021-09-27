import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";
import {createElsaClient} from "../../../../services/elsa-client";
import Tunnel from '../../../../data/workflow-editor';
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";
import Ajv from "ajv"

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
  @State() isJsonSchemaValid?: boolean;
  monacoEditor: HTMLElsaMonacoElement;

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expressions['Literal'];
    this.validate(this.currentValue);
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

  onConvertToJsonSchemaClick(e: Event) {
    e.preventDefault();
    window.open('https://extendsclass.com/json-schema-validator.html');
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {    
    this.currentValue = e.value;
    this.validate(e.value);
  }

  validate(value: string) {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldName = propertyName;    
    
    if (fieldName === "Schema")
    {
      this.isJsonSchemaValid = true;
      const ajv = new Ajv();
      let json: object;
      try{
        json = JSON.parse(value)
      }
      catch (e){
        this.isJsonSchemaValid = false;
      }

      if (json != undefined)
      {
        try {
          const validate = ajv.compile(json);
          const errors = validate.errors;
          if (errors != null)
            this.isJsonSchemaValid = false;
        }
        catch (e){
          const err = e;
          this.isJsonSchemaValid = false;
        }        
      }
    }
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
    const isSchema = fieldName === "Schema";

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
      {this.isJsonSchemaValid == undefined ? undefined : this.isJsonSchemaValid ?
          <p class="elsa-mt-1 elsa-text-sm elsa-text-green-500">
                Json is valid
          </p>
          :
          <p class="elsa-mt-1 elsa-text-sm elsa-text-red-500">
                Json is invalid
          </p>
      }
      <input type="hidden" name={fieldName} value={value}/>
      {isSchema ?
        <div class="elsa-mt-1">
          <a href="#"
              onClick={e => this.onConvertToJsonSchemaClick(e)}
              class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-gray-300 elsa-text-sm elsa-leading-5 elsa-font-medium elsa-rounded-md elsa-text-gray-700 elsa-bg-white hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-shadow-outline-blue focus:elsa-border-blue-300 active:elsa-bg-gray-100 active:elsa-text-gray-700 elsa-transition elsa-ease-in-out elsa-duration-150">
              Convert to Json Schema
          </a>
        </div>
        : undefined
      }
    </div>
  }
}

Tunnel.injectProps(ElsaScriptProperty, ['serverUrl', 'workflowDefinitionId']);
