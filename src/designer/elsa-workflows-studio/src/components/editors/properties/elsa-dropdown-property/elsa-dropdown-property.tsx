import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";

@Component({
  tag: 'elsa-dropdown-property',
  styleUrl: 'elsa-dropdown-property.css',
  shadow: false,
})
export class ElsaDropdownProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() currentValue?: string;

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expression || '';
  }

  onChanged(e: Event) {
    const select = (e.target as HTMLSelectElement);
    this.currentValue = select.value;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const options = propertyDescriptor.options as Array<any> || [];
    const value = this.currentValue;

    return (
      <div>
        <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
          {fieldLabel}
        </label>

        <select id={fieldId} name={fieldName} class="mt-1 block focus:ring-blue-500 focus:border-blue-500 w-full shadow-sm sm:max-w-xs sm:text-sm border-gray-300 rounded-md">
          {options.map((option, index) => <option value={option} selected={option === value}>{option}</option>)}
        </select>

        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
      </div>
    )
  }
}
