import {Component, h, Prop} from '@stencil/core';
import {LiteralExpression, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue} from "../../utils";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";
import descriptorsStore from "../../data/descriptors-store";

@Component({
  tag: 'elsa-single-line-input',
  shadow: false
})
export class SingleLineInput {
  @Prop() public inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const defaultValue = inputDescriptor.defaultValue;
    const value = this.getValueOrDefault((input?.expression as LiteralExpression)?.value, defaultValue); // TODO: The "value" field is currently hardcoded, but we should be able to be more flexible and potentially have different fields for a given syntax.
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;

    return (
      <elsa-input-control-switch label={displayName} hint={hint} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
          <input type="text" name={fieldName} id={fieldId} value={value} onChange={this.onPropertyEditorChanged}/>
      </elsa-input-control-switch>
    );
  }

  private onPropertyEditorChanged = (e: Event) => {
    const inputElement = e.target as HTMLInputElement;
    this.inputContext.inputChanged(inputElement.value, SyntaxNames.Literal);
  }

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }

  private getValueOrDefault(value: string | undefined, defaultValue: string | undefined) {
    return value ?? defaultValue ?? '';
  }
}
