import {Component, h, Prop} from '@stencil/core';
import {EditorHeight, LiteralExpression, SyntaxNames} from "../../models";
import {NodeInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue} from "../../utils";
import {MonacoValueChangedArgs} from "../shared/monaco-editor/monaco-editor";

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
  @Prop() public inputContext: NodeInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const options: CodeEditorOptions = inputDescriptor.options || {};
    const input = getInputPropertyValue(inputContext);
    let currentValue = (input?.expression as LiteralExpression)?.value;

    if (currentValue == undefined)
      currentValue = inputDescriptor.defaultValue;

    return (
      <elsa-monaco-editor value={currentValue} {...options} onValueChanged={this.onChange}/>
    );
  }

  private onChange = (e: CustomEvent<MonacoValueChangedArgs>) => {
    const value = e.detail.value;
    this.inputContext.inputChanged(value, SyntaxNames.Literal);
  }
}
