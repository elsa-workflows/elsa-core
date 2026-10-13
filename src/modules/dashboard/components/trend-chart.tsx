"use client";

import { format, parseISO } from "date-fns";
import { useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import type { DashboardTrendBucket, DashboardTrendGranularity } from "../api";

/**
 * Inline SVG trend chart: stacked outcome bars (finished / faulted / suspended) with the number of
 * started instances drawn as a muted step line, one y-axis, hover tooltip. Colours reuse the status
 * tones of `StatusBadge` so the chart, badges and tiles read as one system in both themes.
 */

type SeriesKey = "finished" | "faulted" | "suspended";

const SERIES: { key: SeriesKey; label: string; fill: string; dot: string }[] = [
  { key: "finished", label: "Finished", fill: "fill-emerald-500 dark:fill-emerald-400", dot: "bg-emerald-500 dark:bg-emerald-400" },
  { key: "suspended", label: "Suspended", fill: "fill-amber-500 dark:fill-amber-400", dot: "bg-amber-500 dark:bg-amber-400" },
  { key: "faulted", label: "Faulted", fill: "fill-red-500 dark:fill-red-400", dot: "bg-red-500 dark:bg-red-400" },
];

const HEIGHT = 168;
const PAD = { top: 10, right: 8, bottom: 22, left: 34 };
const GAP = 2;

function useElementWidth<T extends HTMLElement>() {
  const ref = useRef<T>(null);
  const [width, setWidth] = useState(0);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const ro = new ResizeObserver(([entry]) => setWidth(entry.contentRect.width));
    ro.observe(el);
    setWidth(el.clientWidth);
    return () => ro.disconnect();
  }, []);
  return { ref, width };
}

function labelFor(iso: string, granularity: DashboardTrendGranularity) {
  const d = parseISO(iso);
  return granularity === "day" ? format(d, "EEE d") : format(d, "HH:mm");
}

function topRoundedRect(x: number, y: number, w: number, h: number, r: number) {
  const rr = Math.min(r, w / 2, h);
  return `M${x},${y + h} V${y + rr} Q${x},${y} ${x + rr},${y} H${x + w - rr} Q${x + w},${y} ${x + w},${y + rr} V${y + h} Z`;
}

export function TrendChart({ buckets, granularity, className }: { buckets: DashboardTrendBucket[]; granularity: DashboardTrendGranularity; className?: string }) {
  const { ref, width } = useElementWidth<HTMLDivElement>();
  const [hover, setHover] = useState<number | null>(null);

  const totals = useMemo(
    () =>
      buckets.reduce(
        (acc, b) => ({
          started: acc.started + b.createdOrStarted,
          finished: acc.finished + b.finished,
          faulted: acc.faulted + b.faulted,
          suspended: acc.suspended + b.suspended,
        }),
        { started: 0, finished: 0, faulted: 0, suspended: 0 },
      ),
    [buckets],
  );

  const max = Math.max(1, ...buckets.map((b) => Math.max(b.createdOrStarted, b.finished + b.faulted + b.suspended)));
  const plotW = Math.max(0, width - PAD.left - PAD.right);
  const plotH = HEIGHT - PAD.top - PAD.bottom;
  const n = buckets.length;
  const slot = n > 0 ? plotW / n : 0;
  const barW = Math.max(2, slot - GAP);
  const y = (v: number) => PAD.top + plotH - (v / max) * plotH;
  const x = (i: number) => PAD.left + i * slot + GAP / 2;

  const empty = totals.started + totals.finished + totals.faulted + totals.suspended === 0;
  const ticks = [max, max / 2];
  const xLabels = n === 0 ? [] : [0, Math.floor((n - 1) / 2), n - 1].filter((v, i, a) => a.indexOf(v) === i);

  const startedPath =
    n === 0
      ? ""
      : buckets
          .map((b, i) => `${i === 0 ? "M" : "L"}${x(i)},${y(b.createdOrStarted)} H${x(i) + barW}`)
          .join(" ");

  const hovered = hover !== null ? buckets[hover] : undefined;

  return (
    <div className={cn("flex flex-col gap-2", className)}>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
        <span className="flex items-center gap-1.5">
          <span className="inline-block h-0.5 w-3 bg-foreground/50" aria-hidden />
          Started <span className="font-medium text-foreground tabular-nums">{totals.started.toLocaleString()}</span>
        </span>
        {SERIES.map((s) => (
          <span key={s.key} className="flex items-center gap-1.5">
            <span className={cn("inline-block size-2 rounded-full", s.dot)} aria-hidden />
            {s.label} <span className="font-medium text-foreground tabular-nums">{totals[s.key].toLocaleString()}</span>
          </span>
        ))}
      </div>
      <div ref={ref} className="relative w-full" style={{ height: HEIGHT }}>
        {width > 0 && (
          <svg
            width={width}
            height={HEIGHT}
            role="img"
            aria-label={`Workflow trend, ${n} buckets`}
            className="block select-none"
            onMouseLeave={() => setHover(null)}
            onMouseMove={(e) => {
              const rect = e.currentTarget.getBoundingClientRect();
              const px = e.clientX - rect.left - PAD.left;
              const i = slot > 0 ? Math.floor(px / slot) : -1;
              setHover(i >= 0 && i < n ? i : null);
            }}
          >
            {ticks.map((t) => (
              <g key={t}>
                <line x1={PAD.left} x2={width - PAD.right} y1={y(t)} y2={y(t)} className="stroke-border" strokeDasharray="2 3" />
                <text x={PAD.left - 6} y={y(t) + 3} textAnchor="end" className="fill-muted-foreground text-[10px] tabular-nums">
                  {Number.isInteger(t) ? t : t.toFixed(1)}
                </text>
              </g>
            ))}
            <line x1={PAD.left} x2={width - PAD.right} y1={y(0)} y2={y(0)} className="stroke-border" />

            {buckets.map((b, i) => {
              const segments = SERIES.map((s) => ({ ...s, value: b[s.key] })).filter((s) => s.value > 0);
              let acc = 0;
              const isHover = hover === i;
              return (
                <g key={b.from} className={cn("transition-opacity", hover !== null && !isHover && "opacity-60")}>
                  {segments.map((s, idx) => {
                    const y0 = y(acc);
                    acc += s.value;
                    const y1 = y(acc);
                    const h = Math.max(1, y0 - y1 - (idx > 0 ? GAP : 0));
                    const top = y1;
                    const isTop = idx === segments.length - 1;
                    return isTop ? (
                      <path key={s.key} d={topRoundedRect(x(i), top, barW, h, 3)} className={s.fill} />
                    ) : (
                      <rect key={s.key} x={x(i)} y={top} width={barW} height={h} className={s.fill} />
                    );
                  })}
                  {isHover && (
                    <rect x={x(i) - GAP / 2} y={PAD.top} width={slot} height={plotH} className="fill-foreground/5" />
                  )}
                </g>
              );
            })}

            {startedPath && <path d={startedPath} fill="none" strokeWidth={1.5} className="stroke-foreground/50" />}

            {xLabels.map((i) => (
              <text
                key={i}
                x={x(i) + barW / 2}
                y={HEIGHT - 6}
                textAnchor={i === 0 ? "start" : i === n - 1 ? "end" : "middle"}
                className="fill-muted-foreground text-[10px] tabular-nums"
              >
                {labelFor(buckets[i].from, granularity)}
              </text>
            ))}

            {empty && (
              <text x={PAD.left + plotW / 2} y={PAD.top + plotH / 2} textAnchor="middle" className="fill-muted-foreground text-xs">
                No workflow activity in this range
              </text>
            )}
          </svg>
        )}
        {hovered && hover !== null && (
          <div
            role="tooltip"
            className="pointer-events-none absolute top-0 z-10 w-44 rounded-md border bg-popover p-2 text-xs text-popover-foreground shadow-md"
            style={{ left: Math.min(Math.max(0, x(hover) - 88), Math.max(0, width - 176)) }}
          >
            <div className="mb-1 font-medium">
              {labelFor(hovered.from, granularity)} – {labelFor(hovered.to, granularity)}
            </div>
            <dl className="grid grid-cols-[1fr_auto] gap-x-3 gap-y-0.5 text-muted-foreground">
              <dt>Started</dt>
              <dd className="text-right text-foreground tabular-nums">{hovered.createdOrStarted}</dd>
              {SERIES.map((s) => (
                <TooltipRow key={s.key} label={s.label} value={hovered[s.key]} dot={s.dot} />
              ))}
              <dt>With incidents</dt>
              <dd className="text-right text-foreground tabular-nums">{hovered.incidentBearing}</dd>
            </dl>
          </div>
        )}
      </div>
    </div>
  );
}

function TooltipRow({ label, value, dot }: { label: string; value: number; dot: string }) {
  return (
    <>
      <dt className="flex items-center gap-1.5">
        <span className={cn("inline-block size-1.5 rounded-full", dot)} aria-hidden />
        {label}
      </dt>
      <dd className="text-right text-foreground tabular-nums">{value}</dd>
    </>
  );
}
