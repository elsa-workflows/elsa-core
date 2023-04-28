import {Component, h, Prop} from '@stencil/core';
import {EditorHeight, LiteralExpression, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue} from "../../utils";
import {MonacoValueChangedArgs} from "../shared/monaco-editor/monaco-editor";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";
import {FormEntry} from "../shared/forms/form-entry";

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
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const options: CodeEditorOptions = inputDescriptor.options || {};
    const defaultValue = inputDescriptor.defaultValue;
    const input = getInputPropertyValue(inputContext);
    let value = this.getValueOrDefault((input?.expression as LiteralExpression)?.value, defaultValue);

    if (value == undefined)
      value = inputDescriptor.defaultValue;

    return (
      <FormEntry label={displayName} fieldId={fieldId} hint={hint}>
        <elsa-monaco-editor value={value} {...options} onValueChanged={this.onChange}/>
      </FormEntry>
    );
  }

  private onChange = (e: CustomEvent<MonacoValueChangedArgs>) => {
    const value = e.detail.value;
    this.inputContext.inputChanged(value, SyntaxNames.Literal);
  }

  private getValueOrDefault(value: string | undefined, defaultValue: string | undefined) {
    return value ?? defaultValue ?? '';
  }
}
