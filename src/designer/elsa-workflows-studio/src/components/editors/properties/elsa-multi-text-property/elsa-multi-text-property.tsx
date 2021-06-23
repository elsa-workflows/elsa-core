import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames, SelectListItem} from "../../../../models";
import {parseJson} from "../../../../utils/utils";
import Tunnel from "../../../../data/workflow-editor";
import {getSelectListItems} from "../../../../utils/select-list-items";

@Component({
  tag: 'elsa-multi-text-property',
  shadow: false,
})
export class ElsaMultiTextProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({mutable: true}) serverUrl: string;
  @State() currentValue?: string;

  items: any[];

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expressions[SyntaxNames.Json] || '[]';
  }

  onValueChanged(newValue: Array<string | number | boolean | SelectListItem>) {
    const newValues = newValue.map(item => {
      if (typeof item === 'string') return item;
      if (typeof item === 'number') return item.toString();
      if (typeof item === 'boolean') return item.toString();

      return item.value;
    })

    this.currentValue = JSON.stringify(newValues);
    this.propertyModel.expressions[SyntaxNames.Json] = this.currentValue;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  createKeyValueOptions(options: Array<SelectListItem>) {
    if (options === null)
      return options;

    return options.map(option => typeof option === 'string' ? { text: option, value: option } : option);
  }

  async componentWillRender(){
    this.items = await getSelectListItems(this.serverUrl, this.propertyDescriptor);
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const values = parseJson(this.currentValue);
    const items = this.items;
    const useDropdown = !!propertyDescriptor.options && propertyDescriptor.options.length > 0;
    const propertyOptions = this.createKeyValueOptions(items);

    const elsaInputTags = useDropdown ?
      <elsa-input-tags-dropdown dropdownValues={propertyOptions} values={values} fieldId={fieldId} fieldName={fieldName} onValueChanged={e => this.onValueChanged(e.detail)} /> :
      <elsa-input-tags values={values} fieldId={fieldId} fieldName={fieldName} onValueChanged={e => this.onValueChanged(e.detail)} />;

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            single-line={true}>
        {elsaInputTags}
      </elsa-property-editor>
    )
  }
}

Tunnel.injectProps(ElsaMultiTextProperty, ['serverUrl']);
