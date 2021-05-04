import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";

@Component({
  tag: 'elsa-multi-line-property',
  styleUrl: 'elsa-multi-line-property.css',
  shadow: false,
})
export class ElsaMultiLineProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() currentValue: string;

  getEditorHeight(options: any) {
    const editorHeightName = options.editorHeight || 'Default';

    switch (editorHeightName) {
      case 'Large':
        return {propertyEditor: '20em', textArea: 6}
      case 'Default':
      default:
        return {propertyEditor: '8em', textArea: 3}
    }
  }

  onChange(e: Event) {
    const input = e.currentTarget as HTMLInputElement;
    this.propertyModel.expressions['Literal'] = this.currentValue = input.value;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const options = propertyDescriptor.options || {};
    const editorHeight = this.getEditorHeight(options);
    const context: string = options.context;
    const fieldId = propertyName;
    const fieldName = propertyName;
    let value = this.currentValue;

    if (value == undefined) {
      const defaultValue = this.propertyDescriptor.defaultValue;
      value = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <elsa-property-editor propertyDescriptor={propertyDescriptor}
                            propertyModel={propertyModel}
                            onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                            editor-height={editorHeight.propertyEditor}
                            context={context}>
        <textarea id={fieldId} name={fieldName} value={value} onChange={e => this.onChange(e)} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300" rows={editorHeight.textArea}/>
      </elsa-property-editor>
    );
  }
}
