import {Component, Prop, h} from '@stencil/core';
import {LiteralExpression, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue} from "../../utils";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-multi-line-input',
  shadow: false
})
export class MultiLineInput {
  @Prop() inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const options = inputDescriptor.options || {};
    const editorHeight = this.getEditorHeight(options);
    const defaultValue = inputDescriptor.defaultValue;
    const value = this.getValueOrDefault((input?.expression as LiteralExpression)?.value, defaultValue);
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;
    return (
      <elsa-input-control-switch label={displayName} hint={hint} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
        <textarea name={fieldName} id={fieldId} value={value} rows={editorHeight.textArea} onChange={this.onPropertyEditorChanged}/>
      </elsa-input-control-switch>
    )
  }

  private getEditorHeight = (options: any) => {
    const editorHeightName = options.editorHeight || 'Default';

    switch (editorHeightName) {
      case 'Large':
        return {propertyEditor: '20em', textArea: 10}
      default:
        return {propertyEditor: '15em', textArea: 6}
    }
  };

  private getValueOrDefault(value: string | undefined, defaultValue: string | undefined) {
    return value ?? defaultValue ?? '';
  }

  private onPropertyEditorChanged = (e: Event) => {
    const inputElement = e.target as HTMLTextAreaElement;
    this.inputContext.inputChanged(inputElement.value, SyntaxNames.Literal);
  };

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }
}
