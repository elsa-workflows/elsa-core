"use client";

import { Checkbox } from "@/components/ui/checkbox";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function CheckboxEditor(props: UiHintEditorProps) {
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <label className="flex h-8 items-center gap-2 text-sm">
          <Checkbox checked={value === true || value === "true"} onCheckedChange={(c) => onChange(c === true)} disabled={props.readOnly} />
          <span className="text-muted-foreground">{props.descriptor.description ?? props.descriptor.displayName}</span>
        </label>
      )}
    </ExpressionEditor>
  );
}
