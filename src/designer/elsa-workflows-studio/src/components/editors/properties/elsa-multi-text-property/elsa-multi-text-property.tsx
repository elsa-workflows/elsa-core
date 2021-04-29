import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames, MultiTextDefinition} from "../../../../models";
import {parseJson} from "../../../../utils/utils";

@Component({
  tag: 'elsa-multi-text-property',
  styleUrl: 'elsa-multi-text-property.css',
  shadow: false,
})
export class ElsaMultiTextProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() currentValue?: string;

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expressions[SyntaxNames.Json] || '[]';
  }

  onValueChanged(newValue: Array<string | MultiTextDefinition>) {
    const newValues = newValue.map(dropdown => {
      if (typeof dropdown === 'string') return dropdown;

      return dropdown.value;
    })

    this.currentValue = JSON.stringify(newValues);
    this.propertyModel.expressions[SyntaxNames.Json] = this.currentValue;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  createKeyValueOptions(options: Array<MultiTextDefinition>) {
    if (options === null)
      return options;

    return options.map(option => typeof option === 'string' ? { text: option, value: option } : option);
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const values = parseJson(this.currentValue);
    propertyDescriptor.options = propertyDescriptor.options || null;
    const valueType = propertyDescriptor.options !== null ? 'dropdown' : 'multi-text';
    const propertyOptions = this.createKeyValueOptions(propertyDescriptor.options);

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
