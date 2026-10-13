"use client";

import { X } from "lucide-react";
import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import type { UiHintEditorProps } from "../types";
import { stringListOf } from "../expression";
import { ExpressionEditor } from "../expression-editor";

/** Tag-style list editor used by `multitext`, `http-status-codes` and `dynamic-outcomes`. */
export function MultiTextEditor(props: UiHintEditorProps) {
  const [draft, setDraft] = useState("");
  const numeric = /Int32|Int64|Long/.test(props.descriptor.typeName);
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => {
        const items: (string | number)[] = stringListOf(value, props.descriptor);
        const commit = () => {
          const text = draft.trim();
          if (!text) return;
          const parsed = numeric ? Number(text) : text;
          if (numeric && Number.isNaN(parsed)) return;
          if (!items.includes(parsed)) onChange([...items, parsed]);
          setDraft("");
        };
        return (
          <div className="flex flex-col gap-1.5">
            <div className="flex flex-wrap gap-1">
              {items.map((item, i) => (
                <Badge key={`${item}-${i}`} variant="secondary" className="gap-1 pr-1">
                  {String(item)}
                  {!props.readOnly && (
                    <button type="button" className="rounded hover:bg-muted-foreground/20" onClick={() => onChange(items.filter((_, j) => j !== i))} aria-label="Remove">
                      <X className="size-3" />
                    </button>
                  )}
                </Badge>
              ))}
            </div>
            {!props.readOnly && (
              <Input
                value={draft}
                placeholder="Type and press Enter"
                onChange={(e) => setDraft(e.target.value)}
                onBlur={commit}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === ",") {
                    e.preventDefault();
                    commit();
                  }
                }}
              />
            )}
          </div>
        );
      }}
    </ExpressionEditor>
  );
}
