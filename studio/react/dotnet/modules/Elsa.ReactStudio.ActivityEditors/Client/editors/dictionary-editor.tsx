"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Plus, Trash2 } from "lucide-react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";
import { OBJECT } from "@elsa-workflows/react-studio-sdk/activity-expressions";

/** Key/value table stored as a JSON object literal. */
export function DictionaryEditor(props: UiHintEditorProps) {
  const _t = useTranslation();
  return (
    <ExpressionEditor {...props} literalSyntax={OBJECT}>
      {({ value, onChange }) => {
        const entries = Object.entries((value && typeof value === "object" ? value : {}) as Record<string, unknown>);
        const update = (next: [string, unknown][]) => onChange(Object.fromEntries(next));
        return (
          <div className="flex flex-col gap-1.5">
            {entries.map(([k, v], i) => (
              <div key={i} className="flex items-center gap-1.5">
                <Input value={k} placeholder={_t("Key")} readOnly={props.readOnly} onChange={(e) => update(entries.map((en, j) => (j === i ? [e.target.value, en[1]] : en)))} />
                <Input value={String(v ?? "")} placeholder={_t("Value")} readOnly={props.readOnly} onChange={(e) => update(entries.map((en, j) => (j === i ? [en[0], e.target.value] : en)))} />
                {!props.readOnly && (
                  <Button variant="ghost" size="icon-sm" onClick={() => update(entries.filter((_, j) => j !== i))} aria-label={_t("Remove")}>
                    <Trash2 />
                  </Button>
                )}
              </div>
            ))}
            {!props.readOnly && (
              <Button variant="outline" size="sm" className="self-start" onClick={() => update([...entries, ["", ""]])}>
                <Plus /> {_t("Add entry")}</Button>
            )}
          </div>
        );
      }}
    </ExpressionEditor>
  );
}
