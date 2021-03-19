import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";
import {parseJson} from "../../../../utils/utils";

@Component({
  tag: 'elsa-multi-text-property',
  styleUrl: 'elsa-multi-text-property.css',
  shadow: false,
})
export class ElsaMultiTextProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() values?: Array<string>

  async componentWillLoad() {
    const expression = this.propertyModel.expression || '[]';
    this.values = parseJson(expression) ?? [];
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const values = this.values.map(x => x ? x.trim() : '').filter(x => x.length > 0);

    return (
      <div>
        <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
          {fieldLabel}
        </label>
        <div class="mt-1">
          <elsa-input-tags values={values} fieldId={fieldId} fieldName={fieldName}/>
        </div>
        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
      </div>
    )
  }
}
