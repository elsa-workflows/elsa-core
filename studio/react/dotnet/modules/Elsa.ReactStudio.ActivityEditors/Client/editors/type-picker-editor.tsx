"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { useVariableTypes } from "@elsa-workflows/react-studio-api-client/hooks/descriptors";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function TypePickerEditor(props: UiHintEditorProps) {
  useTranslation();
  const { items } = useVariableTypes();
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <SimpleSelect
          value={typeof value === "string" ? value : undefined}
          allowEmpty
          disabled={props.readOnly}
          options={items.map((t) => ({ value: t.typeName, label: t.displayName }))}
          onChange={(v) => onChange(v)}
        />
      )}
    </ExpressionEditor>
  );
}
