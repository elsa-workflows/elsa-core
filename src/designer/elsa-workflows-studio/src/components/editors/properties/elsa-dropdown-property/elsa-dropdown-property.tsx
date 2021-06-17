import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, RuntimeSelectListItemsProviderSettings, SelectListItem, SyntaxNames} from "../../../../models";
import Tunnel from "../../../../data/workflow-editor";
import {getSelectListItems} from "../../../../utils/select-list-items";

@Component({
  tag: 'elsa-dropdown-property',
  shadow: false,
})
export class ElsaDropdownProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({mutable: true}) serverUrl: string;
  @State() currentValue?: string;
  
  items: any[];

  async componentWillLoad() {
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.currentValue = this.propertyModel.expressions[defaultSyntax] || '';
    this.items = await getSelectListItems(this.serverUrl, this.propertyDescriptor);
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
    const currentValue = this.currentValue;
    const items = this.items;

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            single-line={true}>
        <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)} class="elsa-mt-1 elsa-block focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-w-full elsa-shadow-sm sm:elsa-max-w-xs sm:elsa-text-sm elsa-border-gray-300 elsa-rounded-md">
          {items.map(item => {
            const optionIsObject = typeof (item) == 'object';
            const value = optionIsObject ? item.value : item.toString();
            const text = optionIsObject ? item.text : item.toString();
            return <option value={value} selected={value === currentValue}>{text}</option>;
          })}
        </select>
      </elsa-property-editor>
    );
  }
}

Tunnel.injectProps(ElsaDropdownProperty, ['serverUrl']);