"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import type { UiHintEditorProps } from "../types";

/**
 * Picks a workflow variable. Stored as a literal `{ id, name, typeName }` (the `Variable` CLR type);
 * the server resolves it by id.
 */
export function VariablePickerEditor({ descriptor, value, onChange, context, readOnly }: UiHintEditorProps) {
  const _t = useTranslation();
  const current = value?.expression?.value as { id?: string } | undefined;
  return (
    <SimpleSelect
      value={current?.id}
      allowEmpty
      disabled={readOnly}
      options={context.variables.map((v) => ({ value: v.id, label: _t("{{value0}} ({{value1}})", { value0: v.name, value1: v.typeName }) }))}
      onChange={(id) => {
        const variable = context.variables.find((v) => v.id === id);
        onChange(variable ? { typeName: descriptor.typeName, expression: { type: "Literal", value: { id: variable.id, name: variable.name, typeName: variable.typeName } } } : undefined);
      }}
    />
  );
}
