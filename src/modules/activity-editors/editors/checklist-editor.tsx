"use client";

import { Checkbox } from "@/components/ui/checkbox";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";
import { selectListItems } from "./options";
import { stringListOf } from "../expression";

export function ChecklistEditor(props: UiHintEditorProps) {
  const items = selectListItems(props.descriptor);
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => {
        // The stored value may be an array or the JSON string Elsa normalises lists into.
        const selected = new Set(stringListOf(value, props.descriptor));
        return (
          <div className="flex flex-wrap gap-x-4 gap-y-1.5">
            {items.map((i) => (
              <label key={i.value} className="flex items-center gap-2 text-sm">
                <Checkbox
                  checked={selected.has(i.value)}
                  disabled={props.readOnly}
                  onCheckedChange={(c) => {
                    const next = new Set(selected);
                    if (c) next.add(i.value);
                    else next.delete(i.value);
                    onChange(items.filter((x) => next.has(x.value)).map((x) => x.value));
                  }}
                />
                {i.text}
              </label>
            ))}
          </div>
        );
      }}
    </ExpressionEditor>
  );
}
