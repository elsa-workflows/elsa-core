"use client";

import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Label } from "@/components/ui/label";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";
import { selectListItems } from "./options";

export function RadioListEditor(props: UiHintEditorProps) {
  const items = selectListItems(props.descriptor);
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <RadioGroup value={value === undefined || value === null ? null : String(value)} onValueChange={(v) => onChange(v)} disabled={props.readOnly} className="gap-1.5">
          {items.map((i) => (
            <div key={i.value} className="flex items-center gap-2">
              <RadioGroupItem value={i.value} id={`${props.descriptor.name}-${i.value}`} />
              <Label htmlFor={`${props.descriptor.name}-${i.value}`} className="font-normal">
                {i.text}
              </Label>
            </div>
          ))}
        </RadioGroup>
      )}
    </ExpressionEditor>
  );
}
