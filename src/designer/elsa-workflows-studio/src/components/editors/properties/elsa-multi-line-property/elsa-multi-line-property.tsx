import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityModel, ActivityPropertyDescriptor, PropertySettings, SyntaxNames} from "../../../../models";

@Component({
  tag: 'elsa-multi-line-property',
  shadow: false,
})
export class ElsaMultiLineProperty {

  @Prop() activityModel: ActivityModel;
  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() currentValue: string;

  componentWillLoad() {
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.currentValue = this.propertyModel.expressions[defaultSyntax] || undefined;
  }

  getEditorHeight(options?: PropertySettings) {
    const editorHeightName = options?.editorHeight || 'Default';

    switch (editorHeightName) {
      case 'Large':
        return {propertyEditor: '20em', textArea: 6}
    }
    return {propertyEditor: '15em', textArea: 3}
  }

  onChange(e: Event) {
    const input = e.currentTarget as HTMLTextAreaElement;
    this.propertyModel.expressions['Literal'] = this.currentValue = input.value;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const options = propertyDescriptor.options as PropertySettings;
    const editorHeight = this.getEditorHeight(options);
    const context: string = options?.context;
    const fieldId = propertyName;
    const fieldName = propertyName;
    let value = this.currentValue;

    if (value == undefined) {
      const defaultValue = this.propertyDescriptor.defaultValue;
      value = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <elsa-property-editor
        activityModel={this.activityModel}
        propertyDescriptor={propertyDescriptor}
        propertyModel={propertyModel}
        onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
        editor-height={editorHeight.propertyEditor}
        context={context}>
        <textarea id={fieldId} name={fieldName} value={value} onChange={e => this.onChange(e)}
                  class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"
                  rows={editorHeight.textArea}/>
      </elsa-property-editor>
    );
  }
}
