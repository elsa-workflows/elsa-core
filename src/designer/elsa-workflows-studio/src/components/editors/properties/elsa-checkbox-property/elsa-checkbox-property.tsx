import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";

@Component({
  tag: 'elsa-checkbox-property',
  styleUrl: 'elsa-checkbox-property.css',
  shadow: false,
})
export class ElsaCheckBoxProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() isChecked: boolean

  async componentWillLoad() {
    this.isChecked = (this.propertyModel.expressions[SyntaxNames.Literal] || '').toLowerCase() == 'true';
  }

  onCheckChanged(e: Event) {
    const checkbox = (e.target as HTMLInputElement);
    this.isChecked = checkbox.checked;
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.propertyModel.expressions[defaultSyntax] = this.isChecked.toString();
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.isChecked = (e.detail || '').toLowerCase() == 'true';
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const isChecked = this.isChecked;

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            editor-height="2.75em"
                            single-line={true}
                            showLabel={false}>
        <div class="max-w-lg">
          <div class="relative flex items-start">
            <div class="flex items-center h-5">
              <input id={fieldId} name={fieldName} type="checkbox" checked={isChecked} value={'true'} onChange={e => this.onCheckChanged(e)} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
            </div>
            <div class="ml-3 text-sm">
              <label htmlFor={fieldId} class="font-medium text-gray-700">{fieldLabel}</label>
            </div>
          </div>
        </div>
      </elsa-property-editor>
    )
  }
}
