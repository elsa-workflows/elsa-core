import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";
import {parseJson} from "../../../../utils/utils";
import {getSelectListItems} from "../../../../utils/select-list-items";
import Tunnel from "../../../../data/workflow-editor";
import {ElsaDropdownProperty} from "../elsa-dropdown-property/elsa-dropdown-property";

@Component({
  tag: 'elsa-check-list-property',
  styleUrl: 'elsa-check-list-property.css',
  shadow: false,
})
export class ElsaCheckListProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({mutable: true}) serverUrl: string;
  @State() currentValue?: string;
  
  monacoEditor: HTMLElsaMonacoElement;
  items: any[];

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expressions[SyntaxNames.Json] || '[]';
  }

  onCheckChanged(e: Event) {
    const checkbox = (e.target as HTMLInputElement);
    const checked = checkbox.checked;
    const value = checkbox.value;
    let newValue = parseJson(this.currentValue);

    if(checked)
      newValue = [...newValue, value].distinct();
    else
      newValue = newValue.filter(x => x !== value);

    this.currentValue = JSON.stringify(newValue);
    this.propertyModel.expressions[SyntaxNames.Json] = this.currentValue;
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
    const values = parseJson(this.currentValue) || [];

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            editor-height="2.75em"
                            single-line={true}>
        <div class="max-w-lg space-y-3 my-4">
          {items.map((item, index) => {
            const inputId = `${fieldId}_${index}`;
            const optionIsString = typeof(item) == 'string';
            const value = optionIsString ? item : item.value;
            const text = optionIsString ? item : item.text;
            const isSelected = values.findIndex(x => x == value) >= 0;

            return (
              <div class="relative flex items-start">
                <div class="flex items-center h-5">
                  <input id={inputId} type="checkbox" checked={isSelected} value={value} onChange={e => this.onCheckChanged(e)} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
                </div>
                <div class="ml-3 mt-1 text-sm">
                  <label htmlFor={inputId} class="font-medium text-gray-700">{text}</label>
                </div>
              </div>
            );
          })}
        </div>
      </elsa-property-editor>
    );
  }
}

Tunnel.injectProps(ElsaCheckListProperty, ['serverUrl']);