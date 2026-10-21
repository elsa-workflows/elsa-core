"use client";

import { SimpleSelect } from "@/components/common/simple-select";
import { useVariableTypes } from "@/lib/api/hooks/descriptors";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function TypePickerEditor(props: UiHintEditorProps) {
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
