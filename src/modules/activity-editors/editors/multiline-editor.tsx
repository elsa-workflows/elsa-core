"use client";

import { Textarea } from "@/components/ui/textarea";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function MultiLineEditor(props: UiHintEditorProps) {
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <Textarea value={value === undefined || value === null ? "" : String(value)} onChange={(e) => onChange(e.target.value)} readOnly={props.readOnly} rows={4} />
      )}
    </ExpressionEditor>
  );
}
