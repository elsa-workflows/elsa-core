"use client";

import { SimpleSelect } from "@/components/common/simple-select";
import type { ActivityInput } from "@/lib/api/types/activities";
import type { InputDescriptor } from "@/lib/api/types/descriptors";
import { INPUT, VARIABLE } from "../expression";
import type { UiHintContext } from "../types";

/**
 * Editor for an input bound to a workflow member instead of to text: the `Variable` syntax stores the
 * `Variable` object (resolved by id at runtime) and the `Input` syntax stores the `InputDefinition`
 * (resolved by name). The Blazor studio swaps in the variable / input picker for these syntaxes; this
 * is the same behaviour. Fields the stored value carries beyond what the editor knows, such as
 * `storageDriverTypeName`, are preserved when the same member stays selected.
 */
export function BindingPicker({
  syntax,
  descriptor,
  value,
  onChange,
  context,
  readOnly,
}: {
  syntax: typeof VARIABLE | typeof INPUT;
  descriptor: InputDescriptor;
  value: ActivityInput | undefined;
  onChange: (value: ActivityInput | undefined) => void;
  context?: UiHintContext;
  readOnly?: boolean;
}) {
  const stored = (value?.expression?.value ?? undefined) as Record<string, unknown> | undefined;

  if (syntax === VARIABLE) {
    const variables = context?.variables ?? [];
    const selectedId = typeof stored?.id === "string" ? stored.id : undefined;
    return (
      <SimpleSelect
        value={selectedId}
        allowEmpty
        disabled={readOnly}
        placeholder={variables.length ? "Select a variable…" : "No variables declared"}
        options={variables.map((v) => ({ value: v.id, label: `${v.name} (${v.typeName})` }))}
        onChange={(id) => {
          const variable = variables.find((v) => v.id === id);
          if (!variable) return onChange(undefined);
          // Keep any extra fields (e.g. storageDriverTypeName) when the selection is unchanged.
          const extra = selectedId === variable.id ? stored : undefined;
          onChange({ typeName: descriptor.typeName, expression: { type: VARIABLE, value: { ...extra, id: variable.id, name: variable.name, typeName: variable.typeName } } });
        }}
      />
    );
  }

  const inputs = context?.inputs ?? [];
  const selectedName = typeof stored?.name === "string" ? stored.name : undefined;
  return (
    <SimpleSelect
      value={selectedName}
      allowEmpty
      disabled={readOnly}
      placeholder={inputs.length ? "Select a workflow input…" : "No workflow inputs declared"}
      options={inputs.map((i) => ({ value: i.name, label: i.name }))}
      onChange={(name) => {
        if (!name) return onChange(undefined);
        const extra = selectedName === name ? stored : undefined;
        onChange({ typeName: descriptor.typeName, expression: { type: INPUT, value: { ...extra, name } } });
      }}
    />
  );
}
