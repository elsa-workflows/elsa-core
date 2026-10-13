import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export interface KeyValueRow {
  label: ReactNode;
  value: ReactNode;
}

/** Compact two-column read-only data panel. */
export function KeyValueTable({ rows, className }: { rows: KeyValueRow[]; className?: string }) {
  return (
    <dl className={cn("grid grid-cols-[minmax(8rem,auto)_1fr] gap-x-4 gap-y-2 text-sm", className)}>
      {rows.map((r, i) => (
        <div key={i} className="contents">
          <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground pt-0.5">{r.label}</dt>
          <dd className="min-w-0 break-words">{r.value ?? <span className="text-muted-foreground">–</span>}</dd>
        </div>
      ))}
    </dl>
  );
}
