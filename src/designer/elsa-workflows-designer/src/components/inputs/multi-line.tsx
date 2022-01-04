import {Component, Prop, h} from '@stencil/core';
import {LiteralExpression, SyntaxNames} from "../../models";
import {NodeInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue} from "../../utils";

@Component({
  tag: 'elsa-multi-line-input',
  shadow: false
})
export class MultiLineInput {
  @Prop() inputContext: NodeInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputProperty = inputContext.inputDescriptor;
    const fieldName = inputProperty.name;
    const fieldId = inputProperty.name;
    const input = getInputPropertyValue(inputContext);
    const options = inputProperty.options || {};
    const editorHeight = this.getEditorHeight(options);
    const value = (input?.expression as LiteralExpression)?.value;
    return <textarea name={fieldName} id={fieldId} value={value} rows={editorHeight.textArea} onChange={this.onPropertyEditorChanged}/>
  }

  private getEditorHeight = (options: any) => {
    debugger;
    const editorHeightName = options.editorHeight || 'Default';

    switch (editorHeightName) {
      case 'Large':
        return {propertyEditor: '20em', textArea: 10}
      default:
        return {propertyEditor: '15em', textArea: 6}
    }
  };

  private onPropertyEditorChanged = (e: Event) => {
    const inputElement = e.target as HTMLTextAreaElement;
    this.inputContext.inputChanged(inputElement.value, SyntaxNames.Literal);
  };
}
