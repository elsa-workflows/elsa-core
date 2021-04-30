import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames, SelectListItem} from "../../../../models";
import {parseJson} from "../../../../utils/utils";
import Tunnel from "../../../../data/workflow-editor";
import {getSelectListItems} from "../../../../utils/select-list-items";

@Component({
  tag: 'elsa-multi-text-property',
  styleUrl: 'elsa-multi-text-property.css',
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
    const newValues = newValue.map(dropdown => {
      if (typeof dropdown === 'string') return dropdown;
      if (typeof dropdown === 'number') return dropdown.toString();
      if (typeof dropdown === 'boolean') return dropdown.toString();

      return dropdown.value;
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
    const valueType = propertyDescriptor.options !== null ? 'dropdown' : 'multi-text';
    const propertyOptions = this.createKeyValueOptions(items);

    const elsaInputTags = valueType === 'multi-text' ?
      <elsa-input-tags values={values} fieldId={fieldId} fieldName={fieldName} onValueChanged={e => this.onValueChanged(e.detail)} /> :
      <elsa-input-tags-dropdown dropdownValues={propertyOptions} values={values} fieldId={fieldId} fieldName={fieldName} onValueChanged={e => this.onValueChanged(e.detail)} />;

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            editor-height="2em"
                            single-line={true}>
        {elsaInputTags}
      </elsa-property-editor>
    )
  }
}

Tunnel.injectProps(ElsaMultiTextProperty, ['serverUrl']);
