
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import type { UiHintEditorProps } from "../types";
import { inputSelection, selectedInput } from "./editor-values";

export function InputPickerEditor({ value, descriptor, context, readOnly, onChange }: UiHintEditorProps) {
  const _t = useTranslation();
  const inputs = context.inputs ?? [];
  const selected = selectedInput(value, descriptor);
  return <SimpleSelect value={selected?.name} allowEmpty disabled={readOnly}
    placeholder={inputs.length ? _t("Select a workflow input…") : _t("No workflow inputs declared")}
    options={[...inputs].sort((a, b) => a.name.localeCompare(b.name)).map(input => ({
      value: input.name, label: _t("{{value0}} — {{value1}}", { value0: input.name, value1: input.displayName ?? input.type }),
    }))}
    onChange={name => {
      const input = inputs.find(item => item.name === name);
      // Retain fields from a newer server when reselecting the same definition.
      onChange(inputSelection(input ? { ...(selected?.name === name ? selected : {}), ...input } : undefined, descriptor));
    }} />;
}
