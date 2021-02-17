import {Component, Host, h, Prop, State, Listen, Method} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";

@Component({
  tag: 'elsa-text-property',
  styleUrl: 'elsa-text-property.css',
  shadow: false,
})
export class ElsaTextProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() selectedSyntax?: string;

  componentWillLoad(){
    this.selectedSyntax = this.propertyModel.syntax;
  }

  render() {
    const syntaxes = ['Literal', 'JavaScript', 'Liquid'];
    const selectedSyntax = this.selectedSyntax ?? 'Literal';
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const syntaxFieldId = `${fieldId}Syntax`;
    const syntaxFieldName = `${fieldName}Syntax`;
    const property = this.propertyModel;
    const value = property.expression;

    return (
      <div>
        <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
          {fieldLabel}
        </label>
        <div class="mt-1 relative rounded-md">
          {this.editor(fieldId, fieldName, value, selectedSyntax)}
          <div class="absolute inset-y-0 right-0 flex items-center">
            <label htmlFor={syntaxFieldId} class="sr-only">Syntax</label>
            <select id={syntaxFieldId} name={syntaxFieldName} onChange={e => this.onSyntaxChange(e)}
                    class="focus:ring-indigo-500 focus:border-indigo-500 h-full py-0 pl-2 pr-7 border-transparent bg-transparent text-gray-500 sm:text-sm rounded-md">
              {syntaxes.map(syntax => <option selected={syntax == selectedSyntax}>{syntax}</option>)}
            </select>
          </div>
        </div>
        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
      </div>
    )
  }

  editor(fieldId: string, fieldName: string, value: string, syntax: string) {
    const selectedSyntax = syntax;

    if (selectedSyntax == 'Literal')
      return this.literalInputEditor(fieldId, fieldName, value);

    if (selectedSyntax == 'JavaScript')
      return this.javaScriptInputEditor(fieldId, fieldName, value);

    if (selectedSyntax == 'Liquid')
      return this.liquidInputEditor(fieldId, fieldName, value);
  }

  literalInputEditor(fieldId: string, fieldName: string, value: string) {
    return (<input type="text" id={fieldId} name={fieldName} value={value} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>);
  }

  javaScriptInputEditor(fieldId: string, fieldName: string, value: string) {
    return (<div class="w-4/5 h-16">
      <elsa-monaco/>
    </div>);
  }

  liquidInputEditor(fieldId: string, fieldName: string, value: string) {
    return (<input type="text" id={fieldId} name={fieldName} value={value} class="focus:ring-green-500 focus:border-green-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>);
  }

  onSyntaxChange(e: Event) {
    const selectElement = e.currentTarget as HTMLSelectElement;
    this.selectedSyntax = selectElement.value;
  }

}
