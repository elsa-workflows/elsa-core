import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";

@Component({
  tag: 'elsa-checkbox-property',
  styleUrl: 'elsa-checkbox-property.css',
  shadow: false,
})
export class ElsaCheckBoxProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() isChecked: boolean
  monacoEditor: HTMLElsaMonacoElement;

  async componentWillLoad() {
    this.isChecked = (this.propertyModel.expressions['Literal'] || '').toLowerCase() == 'true';
  }

  onCheckChanged(e: Event) {
    const checkbox = (e.target as HTMLInputElement);
    this.isChecked = checkbox.checked;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const isChecked = this.isChecked;

    return (
      <div>
        <div class="max-w-lg space-y-3 my-4">
          <div class="relative flex items-start">
            <div class="flex items-center h-5">
              <input id={fieldId} name={fieldName} type="checkbox" checked={isChecked} value={'true'} onChange={e => this.onCheckChanged(e)} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
            </div>
            <div class="ml-3 text-sm">
              <label htmlFor={fieldId} class="font-medium text-gray-700">{fieldLabel}</label>
            </div>
          </div>
        </div>

        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
      </div>
    )
  }
}
