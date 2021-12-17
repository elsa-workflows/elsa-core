import {Component, h, Host, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityModel, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";
import Tunnel from "../../../../data/workflow-editor";
import {IconColor, IconName, iconProvider} from "../../../../services/icon-provider"

@Component({
  tag: 'elsa-dictionary-property',
  shadow: false,
})
export class ElsaDictionaryProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop() activityModel: ActivityModel;
  @Prop({mutable: true}) serverUrl: string;
  @State() currentValue: [string, string][];

  async componentWillLoad() {
    this.currentValue = this.jsonToDictionary(this.propertyModel.expressions[SyntaxNames.Json] || null);
    if (this.currentValue.length === 0) this.currentValue = [['', '']];
  }

  jsonToDictionary = (json: string): [string, string][] => {
    if (!json) return [['', '']];

    const parsedValue = JSON.parse(json);

    return Object.keys(parsedValue).map(key => [key, parsedValue[key]]);
  }

  dictionaryToJson = (dictionary: [string, string][]) => {
    const filteredDictionary = this.removeInvalidKeys(dictionary);

    if (filteredDictionary.length === 0) return null;

    return JSON.stringify(Object.fromEntries(filteredDictionary));
  }

  removeInvalidKeys = (dictionary: [string, string][]) => {
    const filteredDictionary = [];

    dictionary.forEach(x => {
      const key = x[0].trim();
      if (key !== '' && !filteredDictionary.some(y => y[0].trim() === key))
        filteredDictionary.push(x);
    });

    return filteredDictionary;
  }

  onRowAdded = () => {
    //changing contents of array won't trigger state change,
    //need to update the reference by creating new array
    this.currentValue = [...this.currentValue, ['', '']];
  }

  onRowDeleted = (index: number) => {
    const newValue = this.currentValue.filter((x, i) => i !== index);

    if (newValue.length === 0) newValue.push(['', '']);

    this.currentValue = newValue;
    this.propertyModel.expressions[SyntaxNames.Json] = this.dictionaryToJson(newValue);
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = this.jsonToDictionary(e.detail);
  }

  onKeyChanged(e: Event, index: number) {
    const input = e.currentTarget as HTMLInputElement;
    this.currentValue[index][0] = input.value
    this.propertyModel.expressions[SyntaxNames.Json] = this.dictionaryToJson(this.currentValue);
  }

  onValueChanged(e: Event, index: number) {
    const input = e.currentTarget as HTMLInputElement;
    this.currentValue[index][1] = input.value
    this.propertyModel.expressions[SyntaxNames.Json] = this.dictionaryToJson(this.currentValue);
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const fieldId = propertyDescriptor.name;
    const items = this.currentValue;

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            activityModel={this.activityModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            single-line={true}>
        {items.map((item, index) => {
          const keyInputId = `${fieldId}_${index}_key}`;
          const valueInputId = `${fieldId}_${index}_value}`;
          const [key, value] = item;
          const isLast = index === (items.length - 1);

          return (
            <div class="elsa-flex elsa-flex-row elsa-justify-between elsa-mb-2">
              <input id={keyInputId} type="text" value={key} onChange={(e) => this.onKeyChanged(e, index)}
                     placeholder="Name"
                     class="disabled:elsa-opacity-50 disabled:elsa-cursor-not-allowed focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-border-gray-300 sm:elsa-text-sm elsa-rounded-md elsa-w-5/12"/>
              <input id={valueInputId} type="text" value={value} onChange={(e) => this.onValueChanged(e, index)}
                     placeholder="Value"
                     class="disabled:elsa-opacity-50 disabled:elsa-cursor-not-allowed focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-border-gray-300 sm:elsa-text-sm elsa-rounded-md elsa-w-5/12"/>
              <div class="elsa-flex elsa-flex-row elsa-justify-between elsa-w-24">
                <button type="button" onClick={() => this.onRowDeleted(index)}>
                  {iconProvider.getIcon(IconName.TrashBinOutline, {color: IconColor.Gray, hoverColor: IconColor.Red})}
                </button>
                {isLast && <button type="button" onClick={this.onRowAdded}>
                  {iconProvider.getIcon(IconName.Plus, {color: IconColor.Gray, hoverColor: IconColor.Green})}
                </button>}
              </div>
            </div>
          );
        })}
      </elsa-property-editor>
    );
  }
}

Tunnel.injectProps(ElsaDictionaryProperty, ['serverUrl']);
