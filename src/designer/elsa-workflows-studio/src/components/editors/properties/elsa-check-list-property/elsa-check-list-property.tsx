import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";
import {parseJson} from "../../../../utils/utils";

@Component({
  tag: 'elsa-check-list-property',
  styleUrl: 'elsa-check-list-property.css',
  shadow: false,
})
export class ElsaSingleLineProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({mutable: true}) serverUrl: string;
  @Prop({mutable: true}) workflowDefinitionId: string;
  @State() currentValues?: Array<string>
  monacoEditor: HTMLElsaMonacoElement;

  async componentWillLoad() {
    const expression = this.propertyModel.expression || '[]';
    this.currentValues = parseJson(expression) ?? [];
  }

  onCheckChanged(e: Event) {
    const checkbox = (e.target as HTMLInputElement);
    const checked = checkbox.checked;
    const value = checkbox.value;

    if(checked)
      this.currentValues = [...this.currentValues, value].distinct();
    else
      this.currentValues = this.currentValues.filter(x => x !== value);
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const options = propertyDescriptor.options as Array<any>;
    const values = this.currentValues.map(x => x ? x.trim() : '').filter(x => x.length > 0);
    const valuesJson = JSON.stringify(values);

    return (
      <div>
        <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
          {fieldLabel}
        </label>

        <div class="max-w-lg space-y-3 my-4">
          {options.map((option, index) => {
            const inputId = `${fieldId}_${index}`;
            const isSelected = values.findIndex(x => x == option) >= 0;

            return (
              <div class="relative flex items-start">
                <div class="flex items-center h-5">
                  <input id={inputId} type="checkbox" checked={isSelected} value={option} onChange={e => this.onCheckChanged(e)} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
                </div>
                <div class="ml-3 text-sm">
                  <label htmlFor={inputId} class="font-medium text-gray-700">{option}</label>
                </div>
              </div>
            );
          })}
        </div>

        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
        <input type="hidden" name={fieldName} value={valuesJson}/>
      </div>
    )
  }
}
