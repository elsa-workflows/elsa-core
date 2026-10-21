"use client";

import { Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { CodeEditor } from "@/components/common/code-editor";
import { SimpleSelect } from "@/components/common/simple-select";
import type { UiHintEditorProps } from "../types";
import { languageForSyntax } from "../expression";

interface SwitchCase {
  label: string;
  condition: { type: string; value: unknown };
  activity?: unknown;
}

/**
 * Cases of `Switch` / `FlowSwitch`: each case has a label (which becomes an outgoing port on
 * FlowSwitch) and a condition expression. The input is not wrapped: the value is the raw list.
 */
export function SwitchCasesEditor({ value, onChange, descriptor, syntaxes, readOnly }: UiHintEditorProps) {
  const cases = (Array.isArray(value) ? value : ((value as { expression?: { value?: unknown } } | undefined)?.expression?.value ?? [])) as SwitchCase[];
  const expressionSyntaxes = syntaxes.filter((s) => s !== "Literal");
  const set = (next: SwitchCase[]) => onChange(next as unknown as UiHintEditorProps["value"]);
  const update = (i: number, patch: Partial<SwitchCase>) => set(cases.map((c, j) => (j === i ? { ...c, ...patch } : c)));
  void descriptor;

  return (
    <div className="flex flex-col gap-2">
      {cases.map((c, i) => (
        <div key={i} className="rounded-md border p-2 flex flex-col gap-1.5">
          <div className="flex items-center gap-1.5">
            <Input value={c.label ?? ""} placeholder="Case label" readOnly={readOnly} onChange={(e) => update(i, { label: e.target.value })} />
            <SimpleSelect
              className="w-32"
              size="sm"
              value={c.condition?.type ?? expressionSyntaxes[0]}
              options={expressionSyntaxes.map((s) => ({ value: s, label: s }))}
              onChange={(type) => update(i, { condition: { type: type ?? "JavaScript", value: c.condition?.value ?? "" } })}
              disabled={readOnly}
            />
            {!readOnly && (
              <Button variant="ghost" size="icon-sm" aria-label="Remove case" onClick={() => set(cases.filter((_, j) => j !== i))}>
                <Trash2 />
              </Button>
            )}
          </div>
          <CodeEditor
            value={typeof c.condition?.value === "string" ? c.condition.value : ""}
            language={languageForSyntax(c.condition?.type)}
            height="auto"
            minimal
            readOnly={readOnly}
            placeholder="Condition expression"
            onChange={(text) => update(i, { condition: { type: c.condition?.type ?? expressionSyntaxes[0] ?? "JavaScript", value: text } })}
          />
        </div>
      ))}
      {!readOnly && (
        <Button variant="outline" size="sm" className="self-start" onClick={() => set([...cases, { label: `Case ${cases.length + 1}`, condition: { type: expressionSyntaxes[0] ?? "JavaScript", value: "" } }])}>
          <Plus /> Add case
        </Button>
      )}
    </div>
  );
}
