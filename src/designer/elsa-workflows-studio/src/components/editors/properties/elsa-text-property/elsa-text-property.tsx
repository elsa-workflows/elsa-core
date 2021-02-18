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
  @State() currentValue?: string

  componentWillLoad() {
    this.selectedSyntax = this.propertyModel.syntax;
    this.currentValue = this.propertyModel.expression;
  }

  onSyntaxChange(e: Event, syntax: string) {
    e.preventDefault();
    this.selectedSyntax = syntax;
  }

  onMonacoValueChanged(newValue: string){
    this.currentValue = newValue;
  }

  render() {
    const syntaxes = ['Literal', 'JavaScript', 'Liquid'].reverse();
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
    const value = this.currentValue;

    return (
      <div>
        <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
          {fieldLabel}
        </label>
        <div class="mb-2">
          <div class="sm:hidden">
            <label htmlFor="tabs" class="sr-only">Select a tab</label>
            <select id="tabs" name="tabs" onChange={e => {this.onSyntaxChange(e, (e.currentTarget as HTMLSelectElement).value)}}
                    class="block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md">
              {syntaxes.map(syntax => {
                const isSelected = syntax == selectedSyntax;
                return (
                  <option selected={isSelected}>{syntax}</option>
                );
              })}
            </select>
          </div>
          <div class="hidden sm:block">
              <nav class="flex flex-row-reverse" aria-label="Tabs">
                {syntaxes.map(syntax => {
                  const isSelected = syntax == selectedSyntax;
                  const className = isSelected ? 'bg-blue-100 text-blue-700' : 'text-gray-500 hover:text-gray-700';

                  return (
                    <a href="#" onClick={e => {
                      this.onSyntaxChange(e, syntax)
                    }} class={`${className} px-3 py-2 font-medium text-sm rounded-md`}>
                      {syntax}
                    </a>
                  );
                })}
              </nav>

          </div>
        </div>
        <div class="border border-gray-200 border-t-0">
          <elsa-monaco value={value} syntax={selectedSyntax} onValueChanged={e => this.onMonacoValueChanged(e.detail)}/>
        </div>
        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
        <input type="hidden" name={fieldName} value={value} />
        <input type="hidden" name={syntaxFieldName} value={selectedSyntax} />
      </div>
    )
  }



}
