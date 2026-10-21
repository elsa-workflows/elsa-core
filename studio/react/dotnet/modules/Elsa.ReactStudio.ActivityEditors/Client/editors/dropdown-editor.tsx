"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";
import { selectListItems } from "./options";

export function DropdownEditor(props: UiHintEditorProps) {
  useTranslation();
  const items = selectListItems(props.descriptor);
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <SimpleSelect
          value={value === undefined || value === null ? undefined : String(value)}
          onChange={(v) => onChange(v)}
          options={items.map((i) => ({ value: i.value, label: i.text }))}
          allowEmpty
          disabled={props.readOnly}
        />
      )}
    </ExpressionEditor>
  );
}
