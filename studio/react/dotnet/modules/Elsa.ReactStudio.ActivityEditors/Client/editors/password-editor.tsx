"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";

import { Input } from "@elsa-workflows/react-studio-ui/input";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function PasswordEditor(props: UiHintEditorProps) {
  useTranslation();
  const { readOnly } = props;
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <Input
          type="password"
          autoComplete="new-password"
          spellCheck={false}
          value={value === undefined || value === null ? "" : String(value)}
          onChange={(e) => onChange(e.target.value)}
          readOnly={readOnly}
        />
      )}
    </ExpressionEditor>
  );
}
