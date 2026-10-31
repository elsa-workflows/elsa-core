"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Textarea } from "@elsa-workflows/react-studio-ui/textarea";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function MultiLineEditor(props: UiHintEditorProps) {
  useTranslation();
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <Textarea value={value === undefined || value === null ? "" : String(value)} onChange={(e) => onChange(e.target.value)} readOnly={props.readOnly} rows={4} />
      )}
    </ExpressionEditor>
  );
}
