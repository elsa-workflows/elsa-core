"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { type CodeLanguage } from "@elsa-workflows/react-studio-ui/common/code-editor";
import type { UiHintEditorProps } from "../types";
import { ExpressionCodeEditor } from "../expression-code-editor";
import { ExpressionEditor } from "../expression-editor";
import { languageForSyntax } from "@elsa-workflows/react-studio-sdk/activity-expressions";
import { codeLanguageOf } from "./options";

/** Code/script inputs (e.g. RunJavaScript.Script): the literal itself is edited with a code editor. */
export function CodeEditorHint(props: UiHintEditorProps) {
  useTranslation();
  const spec = codeLanguageOf(props.descriptor);
  const language = (spec as CodeLanguage | undefined) ?? languageForSyntax(props.descriptor.defaultSyntax);
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <ExpressionCodeEditor context={props.context} propertyName={props.descriptor.name} value={typeof value === "string" ? value : value == null ? "" : JSON.stringify(value, null, 2)} onChange={onChange} language={language} height="14rem" readOnly={props.readOnly} />
      )}
    </ExpressionEditor>
  );
}
