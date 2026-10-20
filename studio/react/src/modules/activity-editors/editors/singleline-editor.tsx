"use client";

import { Input } from "@/components/ui/input";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function SingleLineEditor(props: UiHintEditorProps) {
  const { descriptor, readOnly } = props;
  const numeric = /^(Int32|Int64|Long|Double|Decimal|Single|Int16|Byte)$/.test(descriptor.typeName);
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <Input
          type={numeric ? "number" : "text"}
          value={value === undefined || value === null ? "" : String(value)}
          onChange={(e) => onChange(numeric ? (e.target.value === "" ? undefined : Number(e.target.value)) : e.target.value)}
          readOnly={readOnly}
          placeholder={descriptor.defaultValue !== undefined && descriptor.defaultValue !== null ? String(descriptor.defaultValue) : undefined}
        />
      )}
    </ExpressionEditor>
  );
}
