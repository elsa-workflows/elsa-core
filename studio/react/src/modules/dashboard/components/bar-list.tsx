"use client";

import Link from "next/link";
import { cn } from "@/lib/utils";

export interface BarListRow {
  label: string;
  value: number;
  href?: string;
  /** Optional stacked segments (e.g. running / faulted) drawn inside the bar. */
  segments?: { value: number; color: string; label: string }[];
  hint?: string;
}

/** Horizontal bar list; the longest bar fills the row. Good for "per workflow" comparisons. */
export function BarList({ rows, className, color = "var(--status-success)" }: { rows: BarListRow[]; className?: string; color?: string }) {
  const max = Math.max(1, ...rows.map((r) => r.value));
  return (
    <ol className={cn("flex flex-col gap-2", className)}>
      {rows.map((r) => {
        const width = `${Math.max((r.value / max) * 100, r.value > 0 ? 2 : 0)}%`;
        const bar = (
          <div className="grid grid-cols-[minmax(0,11rem)_1fr_auto] items-center gap-3 text-xs">
            <span className="truncate font-medium" title={r.label}>
              {r.label}
            </span>
            <div className="relative h-4 overflow-hidden rounded-full bg-muted">
              <div className="absolute inset-y-0 left-0 flex overflow-hidden rounded-full" style={{ width, background: color }}>
                {r.segments?.map((seg, i) => (
                  <span
                    key={seg.label}
                    title={`${seg.label}: ${seg.value}`}
                    style={{
                      width: `${r.value ? (seg.value / r.value) * 100 : 0}%`,
                      background: seg.color,
                      // A 2px gap in the surface colour separates touching segments; a stroke around
                      // them would add ink that is not data.
                      boxShadow: i < (r.segments?.length ?? 0) - 1 ? "inset -2px 0 0 var(--card)" : undefined,
                    }}
                  />
                ))}
              </div>
            </div>
            <span className="w-8 text-right tabular-nums font-medium">{r.value}</span>
          </div>
        );
        return (
          <li key={r.label} title={r.hint}>
            {r.href ? (
              <Link href={r.href} className="block rounded-md outline-none transition-opacity hover:opacity-90 focus-visible:ring-2 focus-visible:ring-ring/50">
                {bar}
              </Link>
            ) : (
              bar
            )}
          </li>
        );
      })}
    </ol>
  );
}
