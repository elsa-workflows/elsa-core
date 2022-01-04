import {Component, Prop, h} from '@stencil/core';
import {JsonExpression, LiteralExpression, SyntaxNames} from "../../models";
import {NodeInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue, parseJson} from "../../utils";

@Component({
  tag: 'elsa-multi-text-input',
  shadow: false
})
export class MultiTextInput {
  @Prop() inputContext: NodeInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputProperty = inputContext.inputDescriptor;
    const fieldId = inputProperty.name;
    const input = getInputPropertyValue(inputContext);
    const json = (input?.expression as JsonExpression)?.value;
    const values = parseJson(json);
    return <elsa-input-tags fieldId={fieldId} values={values} onValueChanged={this.onPropertyEditorChanged}/>
  }

  private onPropertyEditorChanged = (e: CustomEvent<Array<string>>) => {
    const json = JSON.stringify(e.detail);
    this.inputContext.inputChanged(json, SyntaxNames.Json);
  };
}
