import {Component, Prop, h, State} from '@stencil/core';
import {LiteralExpression, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue } from "../../utils";
import {ExpressionChangedArs} from "../designer/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-checkbox-input',
  shadow: false
})
export class Checkbox {
  @Prop() public inputContext: ActivityInputContext;
  @State() private isChecked?: boolean;

  public async componentWillLoad() {
    this.isChecked = this.getSelectedValue();
  }

  private getSelectedValue = (): boolean => {
    const input = getInputPropertyValue(this.inputContext);
    return (input?.expression as LiteralExpression)?.value;
  };

  render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const value = (input?.expression as LiteralExpression)?.value;
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;
    const isChecked = this.isChecked;

    return (
      <elsa-input-control-switch label={displayName} hideLabel={true} hint={hint} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
        <div class="flex space-x-1">
          <input type="checkbox" name={fieldName} id={fieldId} value={value} checked={isChecked} onChange={this.onPropertyEditorChanged}/>
          <label htmlFor={fieldId}>{displayName}</label>
        </div>
      </elsa-input-control-switch>
    );
  }

  private onPropertyEditorChanged = (e: Event) => {
    const inputElement = e.target as HTMLInputElement;
    this.isChecked = inputElement.checked;
    this.inputContext.inputChanged(inputElement.checked, SyntaxNames.Literal);
  }

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }
}
