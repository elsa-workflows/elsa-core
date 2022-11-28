import {Component, h, Prop} from '@stencil/core';
import {EditorHeight, LiteralExpression, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue} from "../../utils";
import {MonacoValueChangedArgs} from "../shared/monaco-editor/monaco-editor";
import {ExpressionChangedArs} from "../designer/input-control-switch/input-control-switch";
import descriptorsStore from "../../data/descriptors-store";

interface CodeEditorOptions {
  editorHeight?: EditorHeight;
  language?: string;
  singleLineMode?: boolean;
}

@Component({
  tag: 'elsa-code-editor-input',
  shadow: false
})
export class CodeEditorInput {
  @Prop() public inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const options: CodeEditorOptions = inputDescriptor.options || {};
    const input = getInputPropertyValue(inputContext);
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;
    let value = (input?.expression as LiteralExpression)?.value;

    if (value == undefined)
      value = inputDescriptor.defaultValue;

    return (
      <elsa-input-control-switch label={displayName} hint={hint} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
          <elsa-monaco-editor value={value} {...options} onValueChanged={this.onChange}/>
      </elsa-input-control-switch>
    );
  }

  private onChange = (e: CustomEvent<MonacoValueChangedArgs>) => {
    const value = e.detail.value;
    this.inputContext.inputChanged(value, SyntaxNames.Literal);
  }

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }
}
