import {Component, Prop, h} from '@stencil/core';
import {LiteralExpression, SyntaxNames} from "../../models";
import {NodeInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue} from "../../utils";

@Component({
  tag: 'elsa-single-line-input',
  shadow: false
})
export class SingleLineInput {
  @Prop() inputContext: NodeInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputProperty = inputContext.inputDescriptor;
    const fieldName = inputProperty.name;
    const fieldId = inputProperty.name;
    const input = getInputPropertyValue(inputContext);
    const value = (input?.expression as LiteralExpression)?.value;
    return <input type="text" name={fieldName} id={fieldId} value={value} onChange={this.onPropertyEditorChanged}/>
  }

  private onPropertyEditorChanged = (e: Event) => {
    const inputElement = e.target as HTMLInputElement;
    this.inputContext.inputChanged(inputElement.value, SyntaxNames.Literal);
  }
}
