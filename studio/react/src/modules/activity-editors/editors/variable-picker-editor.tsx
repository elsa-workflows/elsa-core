"use client";

import { SimpleSelect } from "@/components/common/simple-select";
import type { UiHintEditorProps } from "../types";

/**
 * Picks a workflow variable. Stored as a literal `{ id, name, typeName }` (the `Variable` CLR type);
 * the server resolves it by id.
 */
export function VariablePickerEditor({ descriptor, value, onChange, context, readOnly }: UiHintEditorProps) {
  const current = value?.expression?.value as { id?: string } | undefined;
  return (
    <SimpleSelect
      value={current?.id}
      allowEmpty
      disabled={readOnly}
      options={context.variables.map((v) => ({ value: v.id, label: `${v.name} (${v.typeName})` }))}
      onChange={(id) => {
        const variable = context.variables.find((v) => v.id === id);
        onChange(variable ? { typeName: descriptor.typeName, expression: { type: "Literal", value: { id: variable.id, name: variable.name, typeName: variable.typeName } } } : undefined);
      }}
    />
  );
}
