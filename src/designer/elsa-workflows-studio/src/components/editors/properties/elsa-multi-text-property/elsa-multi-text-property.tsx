import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";
import {parseJson} from "../../../../utils/utils";

@Component({
  tag: 'elsa-multi-text-property',
  styleUrl: 'elsa-multi-text-property.css',
  shadow: false,
})
export class ElsaMultiTextProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() currentValue?: string

  async componentWillLoad() {
    this.currentValue = this.propertyModel.expressions[SyntaxNames.Json] || '[]';
  }

  onValueChanged(newValue: Array<string>){
    this.currentValue = JSON.stringify(newValue);
    this.propertyModel.expressions[SyntaxNames.Json] = this.currentValue;
  }

  onLiteralValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const values = parseJson(this.currentValue);

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onLiteralValueChanged={e => this.onLiteralValueChanged(e)}
                            editor-height="2em"
                            single-line={true}>
        <elsa-input-tags values={values} fieldId={fieldId} fieldName={fieldName} onValueChanged={e => this.onValueChanged(e.detail)}/>
      </elsa-property-editor>
    )
  }
}
