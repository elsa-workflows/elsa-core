"use client";

import { cn } from "@/lib/utils";

export interface DonutSlice {
  label: string;
  value: number;
  color: string;
  href?: string;
}

/** Inline SVG donut with a centred total and a legend; colours come from the status tokens. */
export function DonutChart({ slices, total, centerLabel, className }: { slices: DonutSlice[]; total?: number; centerLabel?: string; className?: string }) {
  const sum = total ?? slices.reduce((a, s) => a + s.value, 0);
  const r = 42;
  const c = 2 * Math.PI * r;
  let offset = 0;
  return (
    <div className={cn("flex items-center gap-6", className)}>
      <svg viewBox="0 0 120 120" className="size-36 shrink-0 -rotate-90" role="img" aria-label={`${sum} instances by status`}>
        <circle cx="60" cy="60" r={r} fill="none" stroke="var(--muted)" strokeWidth="14" />
        {sum > 0 &&
          slices
            .filter((s) => s.value > 0)
            .map((s) => {
              const len = (s.value / sum) * c;
              const el = (
                <circle
                  key={s.label}
                  cx="60"
                  cy="60"
                  r={r}
                  fill="none"
                  stroke={s.color}
                  strokeWidth="14"
                  strokeDasharray={`${Math.max(len - 1.5, 0)} ${c - Math.max(len - 1.5, 0)}`}
                  strokeDashoffset={-offset}
                  strokeLinecap="butt"
                >
                  <title>{`${s.label}: ${s.value}`}</title>
                </circle>
              );
              offset += len;
              return el;
            })}
        <g className="rotate-90" style={{ transformOrigin: "60px 60px" }}>
          <text x="60" y="58" textAnchor="middle" className="fill-foreground text-[22px] font-semibold tabular-nums">
            {sum}
          </text>
          <text x="60" y="74" textAnchor="middle" className="fill-muted-foreground text-[9px] uppercase tracking-wider">
            {centerLabel ?? "total"}
          </text>
        </g>
      </svg>
      <ul className="flex min-w-0 flex-1 flex-col gap-1.5 text-sm">
        {slices.map((s) => {
          const pct = sum > 0 ? Math.round((s.value / sum) * 100) : 0;
          const row = (
            <>
              <span className="size-2.5 shrink-0 rounded-full" style={{ background: s.color }} />
              <span className="min-w-0 flex-1 truncate">{s.label}</span>
              <span className="tabular-nums font-medium">{s.value}</span>
              <span className="w-10 text-right text-xs tabular-nums text-muted-foreground">{pct}%</span>
            </>
          );
          return (
            <li key={s.label}>
              {s.href ? (
                <a href={s.href} className="flex items-center gap-2 rounded px-1 py-0.5 hover:bg-accent">
                  {row}
                </a>
              ) : (
                <div className="flex items-center gap-2 px-1 py-0.5">{row}</div>
              )}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
