"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import type { ActivityInput } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { InputDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import { INPUT, VARIABLE } from "@elsa-workflows/react-studio-sdk/activity-expressions";
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
  const _t = useTranslation();
  const stored = (value?.expression?.value ?? undefined) as Record<string, unknown> | undefined;

  if (syntax === VARIABLE) {
    const variables = context?.variables ?? [];
    const selectedId = typeof stored?.id === "string" ? stored.id : undefined;
    return (
      <SimpleSelect
        value={selectedId}
        allowEmpty
        disabled={readOnly}
        placeholder={variables.length ? _t("Select a variable…") : _t("No variables declared")}
        options={variables.map((v) => ({ value: v.id, label: _t("{{value0}} ({{value1}})", { value0: v.name, value1: v.typeName }) }))}
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
      placeholder={inputs.length ? _t("Select a workflow input…") : _t("No workflow inputs declared")}
      options={inputs.map((i) => ({ value: i.name, label: i.name }))}
      onChange={(name) => {
        if (!name) return onChange(undefined);
        const extra = selectedName === name ? stored : undefined;
        onChange({ typeName: descriptor.typeName, expression: { type: INPUT, value: { ...extra, name } } });
      }}
    />
  );
}
