import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import type { UiHintEditorProps } from "../types";
import { inputSelection, selectedInput } from "./editor-values";

export function InputPickerEditor({ value, descriptor, context, readOnly, onChange }: UiHintEditorProps) {
  const inputs = context.inputs ?? [];
  const selected = selectedInput(value, descriptor);
  return <SimpleSelect value={selected?.name} allowEmpty disabled={readOnly}
    placeholder={inputs.length ? "Select a workflow input…" : "No workflow inputs declared"}
    options={[...inputs].sort((a, b) => a.name.localeCompare(b.name)).map(input => ({
      value: input.name, label: `${input.name} — ${input.displayName ?? input.type}`,
    }))}
    onChange={name => {
      const input = inputs.find(item => item.name === name);
      // Retain fields from a newer server when reselecting the same definition.
      onChange(inputSelection(input ? { ...(selected?.name === name ? selected : {}), ...input } : undefined, descriptor));
    }} />;
}
