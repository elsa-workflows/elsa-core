import {Component, Prop, h} from '@stencil/core';
import {JsonExpression, LiteralExpression, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue, parseJson} from "../../utils";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-multi-text-input',
  shadow: false
})
export class MultiTextInput {
  @Prop() inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;
    const json = (input?.expression as JsonExpression)?.value;
    const values = parseJson(json);

    return (
      <elsa-input-control-switch label={displayName} hint={hint} syntax={syntax} expression={json} onExpressionChanged={this.onExpressionChanged}>
        <elsa-input-tags fieldId={fieldId} values={values} onValueChanged={this.onPropertyEditorChanged}/>
      </elsa-input-control-switch>
    );
  }

  private onPropertyEditorChanged = (e: CustomEvent<Array<string>>) => {
    const json = JSON.stringify(e.detail);
    this.inputContext.inputChanged(json, SyntaxNames.Json);
  };

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }
}
