import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";

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
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.currentValue = this.propertyModel.expressions[defaultSyntax] || '';
  }

  onChange(e: Event) {
    const select = (e.target as HTMLSelectElement);
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.propertyModel.expressions[defaultSyntax] = this.currentValue = select.value;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const options = propertyDescriptor.options as Array<any> || [];
    const currentValue = this.currentValue;

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            editor-height="2.75em"
                            single-line={true}>
        <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)} class="mt-1 block focus:ring-blue-500 focus:border-blue-500 w-full shadow-sm sm:max-w-xs sm:text-sm border-gray-300 rounded-md">
          {options.map(option => {
            const optionIsString = typeof(option) == 'string';
            const value = optionIsString ? option : option.value;
            const text = optionIsString ? option : option.text;
            return <option value={value} selected={value === currentValue}>{text}</option>;
          })}
        </select>
      </elsa-property-editor>
    );
  }
}
