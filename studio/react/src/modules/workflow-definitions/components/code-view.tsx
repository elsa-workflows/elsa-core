"use client";

import { useState } from "react";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { CodeEditor } from "@/components/common/code-editor";
import type { WorkflowDefinition } from "@/lib/api/types/workflow-definitions";

/** JSON view of the whole definition; edits can be applied back to the editor. */
export function CodeView({ definition, onApply, readOnly }: { definition: WorkflowDefinition; onApply: (next: WorkflowDefinition) => void; readOnly: boolean }) {
  const serialized = JSON.stringify(definition, null, 2);
  const [text, setText] = useState(serialized);
  const [lastSerialized, setLastSerialized] = useState(serialized);
  const [autoApply, setAutoApply] = useState(true);
  const [error, setError] = useState<string>();
  if (serialized !== lastSerialized) {
    setLastSerialized(serialized);
    setText(serialized);
  }

  const apply = (value: string) => {
    try {
      const parsed = JSON.parse(value) as WorkflowDefinition;
      if (!parsed.root) throw new Error("The definition needs a root activity.");
      setError(undefined);
      onApply(parsed);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <div className="flex h-full min-h-0 flex-col gap-2 p-2">
      <div className="flex items-center gap-3 text-sm">
        {!readOnly && (
          <label className="flex items-center gap-2">
            <Checkbox checked={autoApply} onCheckedChange={(c) => setAutoApply(c === true)} /> Apply changes automatically
          </label>
        )}
        {!readOnly && !autoApply && (
          <Button size="sm" variant="outline" onClick={() => apply(text)} disabled={text === serialized}>
            <Check /> Apply
          </Button>
        )}
        {error && <span className="text-xs text-destructive">{error}</span>}
      </div>
      <CodeEditor
        value={text}
        language="json"
        height="100%"
        className="min-h-0 flex-1"
        readOnly={readOnly}
        onChange={(v) => {
          setText(v);
          if (autoApply && !readOnly) apply(v);
        }}
      />
    </div>
  );
}
