import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";
import {parseJson} from "../../../../utils/utils";
import {getSelectListItems} from "../../../../utils/select-list-items";
import Tunnel from "../../../../data/workflow-editor";

@Component({
  tag: 'elsa-radio-list-property',
  shadow: false,
})
export class ElsaRadioListProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({mutable: true}) serverUrl: string;
  @State() currentValue?: string;
  
  monacoEditor: HTMLElsaMonacoElement;
  items: any[];

  async componentWillLoad() {
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.currentValue = this.propertyModel.expressions[defaultSyntax] || null;
  }

  onCheckChanged(e: Event) {
    const radio = (e.target as HTMLInputElement);
    const checked = radio.checked;
    
    if(checked)
      this.currentValue = radio.value;

    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.propertyModel.expressions[defaultSyntax] = this.currentValue;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  async componentWillRender(){
    this.items = await getSelectListItems(this.serverUrl, this.propertyDescriptor);
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const fieldId = propertyDescriptor.name;
    const items = this.items;
    const currentValue = this.currentValue;

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            single-line={true}>
        <div class="elsa-max-w-lg elsa-space-y-3 elsa-my-4">
          {items.map((item, index) => {
            const inputId = `${fieldId}_${index}`;
            const optionIsString = typeof(item) == 'string';
            const value = optionIsString ? item : item.value;
            const text = optionIsString ? item : item.text;
            const isSelected = currentValue == value;

            return (
              <div class="elsa-relative elsa-flex elsa-items-start">
                <div class="elsa-flex elsa-items-center elsa-h-5">
                  <input id={inputId} type="radio" radioGroup={fieldId} checked={isSelected} value={value} onChange={e => this.onCheckChanged(e)} class="elsa-focus:ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300"/>
                </div>
                <div class="elsa-ml-3 elsa-mt-1 elsa-text-sm">
                  <label htmlFor={inputId} class="elsa-font-medium elsa-text-gray-700">{text}</label>
                </div>
              </div>
            );
          })}
        </div>
      </elsa-property-editor>
    );
  }
}

Tunnel.injectProps(ElsaRadioListProperty, ['serverUrl']);