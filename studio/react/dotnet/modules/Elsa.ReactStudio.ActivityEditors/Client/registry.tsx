"use client";

import type { ComponentType } from "react";
import { useExtensions } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
import type { UiHintEditorProps } from "./types";
import { CheckboxEditor } from "./editors/checkbox-editor";
import { ChecklistEditor } from "./editors/checklist-editor";
import { CodeEditorHint } from "./editors/code-editor-hint";
import { DictionaryEditor } from "./editors/dictionary-editor";
import { DropdownEditor } from "./editors/dropdown-editor";
import { JsonEditorHint } from "./editors/json-editor-hint";
import { MultiLineEditor } from "./editors/multiline-editor";
import { MultiTextEditor } from "./editors/multitext-editor";
import { OutcomePickerEditor } from "./editors/outcome-picker-editor";
import { OutputPickerEditor } from "./editors/output-picker-editor";
import { RadioListEditor } from "./editors/radiolist-editor";
import { SingleLineEditor } from "./editors/singleline-editor";
import { SwitchCasesEditor } from "./editors/switch-cases-editor";
import { TypePickerEditor } from "./editors/type-picker-editor";
import { VariablePickerEditor } from "./editors/variable-picker-editor";
import { WorkflowDefinitionPickerEditor } from "./editors/workflow-definition-picker-editor";
import { DateTimePickerEditor } from "./editors/datetime-picker-editor";
import { DynamicOutcomesEditor } from "./editors/dynamic-outcomes-editor";
import { HttpStatusCodesEditor } from "./editors/http-status-codes-editor";
import { InputPickerEditor } from "./editors/input-picker-editor";

/** Built-in editors keyed by the server's UI hint names (`InputUIHints`). */
export const builtInEditors: Record<string, ComponentType<UiHintEditorProps>> = {
  singleline: SingleLineEditor,
  multiline: MultiLineEditor,
  checkbox: CheckboxEditor,
  dropdown: DropdownEditor,
  checklist: ChecklistEditor,
  radiolist: RadioListEditor,
  multitext: MultiTextEditor,
  "http-status-codes": HttpStatusCodesEditor,
  "dynamic-outcomes": DynamicOutcomesEditor,
  "code-editor": CodeEditorHint,
  "expression-editor": CodeEditorHint,
  "json-editor": JsonEditorHint,
  dictionary: DictionaryEditor,
  "variable-picker": VariablePickerEditor,
  "output-picker": OutputPickerEditor,
  "outcome-picker": OutcomePickerEditor,
  "type-picker": TypePickerEditor,
  "workflow-definition-picker": WorkflowDefinitionPickerEditor,
  "switch-editor": SwitchCasesEditor,
  "flow-switch-editor": SwitchCasesEditor,
  "datetime-picker": DateTimePickerEditor,
  "input-picker": InputPickerEditor,
};

/** Resolves the editor for a UI hint, letting modules override or add hints through `extensions.uiHints`. */
export function useUiHintEditor(uiHint: string | undefined): ComponentType<UiHintEditorProps> {
  const extensions = useExtensions("uiHints");
  const hint = uiHint ?? "singleline";
  return extensions[hint] ?? SingleLineEditor;
}
