"use client";

import { format, parseISO } from "date-fns";
import { useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import type { DashboardTrendBucket, DashboardTrendGranularity } from "../api";

/**
 * Inline SVG trend chart: one stacked column per bucket for the outcomes recorded in it
 * (finished / suspended / faulted), one y-axis, hover tooltip. Colours reuse the status tones of
 * `StatusBadge` so the chart, badges and tiles read as one system in both themes.
 *
 * Started is deliberately NOT plotted. It counts a different population: a workflow started in one
 * bucket can finish in a later one, so an hour can record more outcomes than starts (observed: 11
 * started, 12 outcomes). Drawing the columns inside a "started" envelope claims a part-of-whole
 * relationship that does not hold, and the stack visibly overflows its own container when it breaks.
 * The per-bucket figure stays in the tooltip, where it reads as a separate number.
 *
 * Mark geometry follows the house chart rules: columns capped at 24px and centred in their slot
 * rather than filling it, a 2px surface gap between stacked segments, a 4px rounded cap on the top
 * segment and a square foot on the baseline, and solid hairline gridlines on whole-number ticks.
 */

type SeriesKey = "finished" | "faulted" | "suspended";

const SERIES: { key: SeriesKey; label: string; fill: string; dot: string }[] = [
  { key: "finished", label: "Finished", fill: "fill-emerald-500 dark:fill-emerald-400", dot: "bg-emerald-500 dark:bg-emerald-400" },
  { key: "suspended", label: "Suspended", fill: "fill-amber-500 dark:fill-amber-400", dot: "bg-amber-500 dark:bg-amber-400" },
  { key: "faulted", label: "Faulted", fill: "fill-red-500 dark:fill-red-400", dot: "bg-red-500 dark:bg-red-400" },
];

const HEIGHT = 208;
const PAD = { top: 12, right: 8, bottom: 24, left: 36 };
/** The surface gap that separates touching marks: between stacked segments and between columns. */
const GAP = 2;
/** Columns never fill their slot; the leftover band is deliberate air. */
const MAX_BAR_W = 24;

/** A whole-number axis step, so a count axis never reads "8.5". */
function niceStep(raw: number) {
  const magnitude = 10 ** Math.floor(Math.log10(Math.max(1, raw)));
  for (const m of [1, 2, 2.5, 5, 10]) {
    const step = m * magnitude;
    if (step >= raw) return Math.max(1, Math.round(step));
  }
  return Math.max(1, Math.round(10 * magnitude));
}

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

  const peak = Math.max(1, ...buckets.map((b) => b.finished + b.faulted + b.suspended));
  // Aim for about three intervals: halving leaves an axis that tops out far above the tallest column.
  const step = niceStep(peak / 3);
  // The axis tops out on a tick, so the highest column stops at a gridline instead of mid-air.
  const axisMax = Math.ceil(peak / step) * step;
  const plotW = Math.max(0, width - PAD.left - PAD.right);
  const plotH = HEIGHT - PAD.top - PAD.bottom;
  const n = buckets.length;
  const slot = n > 0 ? plotW / n : 0;
  const barW = Math.max(2, Math.min(MAX_BAR_W, slot - GAP));
  const y = (v: number) => PAD.top + plotH - (v / axisMax) * plotH;
  /** Left edge of the slot; columns are centred inside it. */
  const slotX = (i: number) => PAD.left + i * slot;
  const x = (i: number) => slotX(i) + (slot - barW) / 2;

  const empty = totals.started + totals.finished + totals.faulted + totals.suspended === 0;
  const ticks = Array.from({ length: Math.floor(axisMax / step) + 1 }, (_, i) => i * step).filter((t) => t > 0);
  const xLabels = n === 0 ? [] : [0, Math.floor((n - 1) / 2), n - 1].filter((v, i, a) => a.indexOf(v) === i);

  const hovered = hover !== null ? buckets[hover] : undefined;
  // Most buckets in a quiet range are empty; a tooltip listing five zeros is noise, not information.
  const hoveredIsQuiet = !!hovered && hovered.createdOrStarted + hovered.finished + hovered.faulted + hovered.suspended + hovered.incidentBearing === 0;

  return (
    <div className={cn("flex flex-col gap-2", className)}>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
        <span className="flex items-center gap-1.5 border-r pr-4">
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
                <line x1={PAD.left} x2={width - PAD.right} y1={y(t)} y2={y(t)} className="stroke-border" />
                <text x={PAD.left - 6} y={y(t) + 3} textAnchor="end" className="fill-muted-foreground text-[10px] tabular-nums">
                  {t.toLocaleString()}
                </text>
              </g>
            ))}
            <line x1={PAD.left} x2={width - PAD.right} y1={y(0)} y2={y(0)} className="stroke-border" />
            {/* A crosshair, not a filled band: a full-height band in an empty hour reads as a column of its own. */}
            {hover !== null && (
              <line x1={slotX(hover) + slot / 2} x2={slotX(hover) + slot / 2} y1={PAD.top} y2={y(0)} className="stroke-foreground/30" />
            )}

            {buckets.map((b, i) => {
              const segments = SERIES.map((s) => ({ ...s, value: b[s.key] })).filter((s) => s.value > 0);
              let acc = 0;
              return (
                <g key={b.from}>
                  {segments.map((s, idx) => {
                    const y0 = y(acc);
                    acc += s.value;
                    const y1 = y(acc);
                    // Every segment but the lowest gives up 2px at its foot, so the surface does the separating.
                    const h = Math.max(1, y0 - y1 - (idx > 0 ? GAP : 0));
                    const isTop = idx === segments.length - 1;
                    return isTop ? (
                      <path key={s.key} d={topRoundedRect(x(i), y1, barW, h, 4)} className={s.fill} />
                    ) : (
                      <rect key={s.key} x={x(i)} y={y1} width={barW} height={h} className={s.fill} />
                    );
                  })}
                </g>
              );
            })}

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
            style={hover < n / 2 ? { right: PAD.right } : { left: PAD.left }}
          >
            <div className={cn("font-medium", !hoveredIsQuiet && "mb-1")}>
              {labelFor(hovered.from, granularity)} – {labelFor(hovered.to, granularity)}
            </div>
            {hoveredIsQuiet ? (
              <p className="text-muted-foreground">No activity</p>
            ) : (
              <dl className="grid grid-cols-[1fr_auto] gap-x-3 gap-y-0.5 text-muted-foreground">
                <dt>Started</dt>
                <dd className="text-right text-foreground tabular-nums">{hovered.createdOrStarted}</dd>
                {SERIES.map((s) => (
                  <TooltipRow key={s.key} label={s.label} value={hovered[s.key]} dot={s.dot} />
                ))}
                <dt>With incidents</dt>
                <dd className="text-right text-foreground tabular-nums">{hovered.incidentBearing}</dd>
              </dl>
            )}
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
