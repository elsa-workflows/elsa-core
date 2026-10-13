"use client";

import { cn } from "@/lib/utils";

export interface FilterChip<T extends string> {
  value: T;
  label: string;
  count?: number;
}

/** Single-choice chip row used for quick list filters (All / Published / Drafts…). */
export function FilterChips<T extends string>({ value, options, onChange, className }: { value: T; options: FilterChip<T>[]; onChange: (v: T) => void; className?: string }) {
  return (
    <div className={cn("inline-flex items-center gap-1 rounded-lg border bg-card p-0.5", className)} role="tablist">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          role="tab"
          aria-selected={value === o.value}
          onClick={() => onChange(o.value)}
          className={cn(
            "rounded-md px-2.5 py-1 text-xs font-medium transition-colors",
            value === o.value ? "bg-accent text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
          )}
        >
          {o.label}
          {o.count !== undefined && <span className="ml-1 tabular-nums opacity-70">{o.count}</span>}
        </button>
      ))}
    </div>
  );
}
