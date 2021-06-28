import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";

@Component({
  tag: 'elsa-json-property',
  shadow: false,
})
export class ElsaJsonProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() currentValue: string;

  componentWillLoad() {
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Json;
    this.currentValue = this.propertyModel.expressions[defaultSyntax] || undefined;
  }

  getEditorHeight(options: any) {
    const editorHeightName = options.editorHeight || 'Large';

    switch (editorHeightName) {
      case 'Large':
        return '20em';
      case 'Default':
      default:
        return '15em';
    }
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    this.propertyModel.expressions[SyntaxNames.Json] = this.currentValue = e.value;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const options = propertyDescriptor.options || {};
    const editorHeight = this.getEditorHeight(options);
    const context: string = options.context;
    let value = this.currentValue;

    if (value == undefined) {
      const defaultValue = this.propertyDescriptor.defaultValue;
      value = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            editor-height={editorHeight}
                            context={context}>
        <elsa-monaco value={value}
                     language="json"
                     editor-height={editorHeight}
                     onValueChanged={e => this.onMonacoValueChanged(e.detail)}
        />
      </elsa-property-editor>
    );
  }
}
