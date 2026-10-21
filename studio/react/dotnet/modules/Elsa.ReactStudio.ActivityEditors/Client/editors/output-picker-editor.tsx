"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

/** Picks one of the workflow's declared outputs by name (used by SetOutput). */
export function OutputPickerEditor(props: UiHintEditorProps) {
  const _t = useTranslation();
  const outputs = (props.context.activity.__workflowOutputs as { name: string }[] | undefined) ?? [];
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <SimpleSelect
          value={typeof value === "string" ? value : undefined}
          allowEmpty
          disabled={props.readOnly}
          options={outputs.map((o) => ({ value: o.name, label: o.name }))}
          onChange={(v) => onChange(v)}
          placeholder={outputs.length ? _t("Select output") : _t("No workflow outputs defined")}
        />
      )}
    </ExpressionEditor>
  );
}
