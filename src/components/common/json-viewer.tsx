"use client";

import { CopyButton } from "./copy-button";
import { cn } from "@/lib/utils";

/** Pretty-printed JSON in a scrollable monospace block with a copy button. */
export function JsonViewer({ value, className, maxHeight = "24rem" }: { value: unknown; className?: string; maxHeight?: string }) {
  const text = typeof value === "string" ? value : JSON.stringify(value, null, 2);
  return (
    <div className={cn("relative rounded-md border bg-muted/40", className)}>
      <div className="absolute right-1 top-1">
        <CopyButton value={text ?? ""} />
      </div>
      <pre className="overflow-auto p-3 text-xs font-mono leading-relaxed" style={{ maxHeight }}>
        {text}
      </pre>
    </div>
  );
}
