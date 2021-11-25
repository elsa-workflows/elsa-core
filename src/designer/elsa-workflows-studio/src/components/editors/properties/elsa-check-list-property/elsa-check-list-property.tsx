import {Component, h, Prop, State} from '@stencil/core';
import {
  ActivityDefinitionProperty,
  ActivityModel,
  ActivityPropertyDescriptor,
  SelectList,
  SyntaxNames
} from "../../../../models";
import {parseJson} from "../../../../utils/utils";
import {getSelectListItems} from "../../../../utils/select-list-items";
import Tunnel from "../../../../data/workflow-editor";

@Component({
  tag: 'elsa-check-list-property',
  shadow: false,
})
export class ElsaCheckListProperty {

  @Prop() activityModel: ActivityModel;
  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({mutable: true}) serverUrl: string;
  @State() currentValue?: string;

  monacoEditor: HTMLElsaMonacoElement;
  selectList: SelectList = {items: [], isFlagsEnum: false};

  async componentWillLoad() {
    if (this.propertyModel.expressions[SyntaxNames.Json] == undefined)
      this.propertyModel.expressions[SyntaxNames.Json] = JSON.stringify(this.propertyDescriptor.defaultValue);
    this.currentValue = this.propertyModel.expressions[SyntaxNames.Json] || '[]';
  }

  onCheckChanged(e: Event) {
    const checkbox = (e.target as HTMLInputElement);
    const checked = checkbox.checked;
    const value = checkbox.value;
    const isFlags = this.selectList.isFlagsEnum;

    if (isFlags) {
      let newValue = parseInt(this.currentValue as string);

      if (checked)
        newValue = newValue | parseInt(value);
      else
        newValue = newValue & ~parseInt(value);

      this.currentValue = newValue.toString();

    } else {
      let newValue = parseJson(this.currentValue as string);

      if (checked)
        newValue = [...newValue, value].distinct();
      else
        newValue = newValue.filter(x => x !== value);

      this.currentValue = JSON.stringify(newValue);
    }

    this.propertyModel.expressions[SyntaxNames.Json] = this.currentValue.toString();
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  async componentWillRender() {
    this.selectList = await getSelectListItems(this.serverUrl, this.propertyDescriptor);
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const fieldId = propertyDescriptor.name;
    const selectList = this.selectList;
    const items = selectList.items;
    const selectedValues = selectList.isFlagsEnum ? this.currentValue : parseJson(this.currentValue as string) || [];

    return (
      <elsa-property-editor
        activityModel={this.activityModel}
        propertyDescriptor={propertyDescriptor}
        propertyModel={propertyModel}
        onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
        single-line={true}>
        <div class="elsa-max-w-lg elsa-space-y-3 elsa-my-4">
          {items.map((item, index) => {
            const inputId = `${fieldId}_${index}`;
            const optionIsString = typeof (item) == 'string';
            const value = optionIsString ? item : item.value;
            const text = optionIsString ? item : item.text;
            const isSelected = selectList.isFlagsEnum
              ? ((parseInt(this.currentValue)) & (parseInt(value as string))) == parseInt(value as string)
              : selectedValues.findIndex(x => x == value) >= 0;

            return (
              <div class="elsa-relative elsa-flex elsa-items-start">
                <div class="elsa-flex elsa-items-center elsa-h-5">
                  <input id={inputId} type="checkbox" checked={isSelected} value={value}
                         onChange={e => this.onCheckChanged(e)}
                         class="focus:elsa-ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300 elsa-rounded"/>
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

Tunnel.injectProps(ElsaCheckListProperty, ['serverUrl']);
