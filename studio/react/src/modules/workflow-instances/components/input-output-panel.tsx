"use client";

import { JsonViewer } from "@/components/common/json-viewer";

function Block({ title, value }: { title: string; value: Record<string, unknown> | undefined }) {
  const empty = !value || Object.keys(value).length === 0;
  return (
    <div className="flex flex-col gap-1.5">
      <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{title}</h4>
      {empty ? <p className="text-sm text-muted-foreground">None.</p> : <JsonViewer value={value} maxHeight="24rem" />}
    </div>
  );
}

export function InputOutputPanel({ input, output }: { input?: Record<string, unknown>; output?: Record<string, unknown> }) {
  return (
    <div className="flex flex-col gap-4 p-3">
      <Block title="Input" value={input} />
      <Block title="Output" value={output} />
    </div>
  );
}
