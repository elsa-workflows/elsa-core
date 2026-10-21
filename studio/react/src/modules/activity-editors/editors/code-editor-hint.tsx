"use client";

import { CodeEditor, type CodeLanguage } from "@/components/common/code-editor";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";
import { languageForSyntax } from "../expression";
import { codeLanguageOf } from "./options";

/** Code/script inputs (e.g. RunJavaScript.Script): the literal itself is edited with a code editor. */
export function CodeEditorHint(props: UiHintEditorProps) {
  const spec = codeLanguageOf(props.descriptor);
  const language = (spec as CodeLanguage | undefined) ?? languageForSyntax(props.descriptor.defaultSyntax);
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <CodeEditor value={typeof value === "string" ? value : value == null ? "" : JSON.stringify(value, null, 2)} onChange={onChange} language={language} height="14rem" readOnly={props.readOnly} />
      )}
    </ExpressionEditor>
  );
}
