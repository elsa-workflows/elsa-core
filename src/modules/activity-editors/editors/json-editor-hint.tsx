"use client";

import { useState } from "react";
import { CodeEditor } from "@/components/common/code-editor";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";
import { OBJECT } from "../expression";

/** JSON object literal; the stored expression type is `Object` with the parsed value. */
export function JsonEditorHint(props: UiHintEditorProps) {
  return (
    <ExpressionEditor {...props} literalSyntax={OBJECT}>
      {({ value, onChange }) => <JsonLiteral value={value} onChange={onChange} readOnly={props.readOnly} />}
    </ExpressionEditor>
  );
}

function JsonLiteral({ value, onChange, readOnly }: { value: unknown; onChange: (v: unknown) => void; readOnly?: boolean }) {
  const external = value === undefined ? "" : typeof value === "string" ? value : JSON.stringify(value, null, 2);
  const [text, setText] = useState(external);
  const [lastExternal, setLastExternal] = useState(external);
  const [error, setError] = useState<string>();
  if (external !== lastExternal) {
    // The value changed from outside (undo, code view): adopt it unless the draft already encodes it.
    setLastExternal(external);
    let same = false;
    try {
      same = !!text && JSON.stringify(JSON.parse(text)) === JSON.stringify(value);
    } catch {
      same = false;
    }
    if (!same) setText(external);
  }
  return (
    <div className="flex flex-col gap-1">
      <CodeEditor
        value={text}
        language="json"
        height="10rem"
        readOnly={readOnly}
        onChange={(t) => {
          setText(t);
          if (!t.trim()) {
            setError(undefined);
            onChange(undefined);
            return;
          }
          try {
            onChange(JSON.parse(t));
            setError(undefined);
          } catch (e) {
            setError((e as Error).message);
          }
        }}
      />
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}
