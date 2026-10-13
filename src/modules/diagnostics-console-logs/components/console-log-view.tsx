"use client";

import { useEffect, useRef } from "react";
import { cn } from "@/lib/utils";
import { formatTimestamp } from "@/components/common/timestamp";
import { sourceIdOf, streamName, stripAnsi, type ConsoleLogLine, type ConsoleLogSource } from "../api";

/** Monospace log listing. Plain scrolling; sticks to the bottom while following. */
export function ConsoleLogView({
  lines,
  sources,
  wrap,
  compact,
  follow,
  className,
}: {
  lines: ConsoleLogLine[];
  sources: ConsoleLogSource[];
  wrap: boolean;
  compact: boolean;
  follow: boolean;
  className?: string;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const names = new Map(sources.map((s) => [s.id, s.displayName]));

  useEffect(() => {
    if (!follow || !ref.current) return;
    ref.current.scrollTop = ref.current.scrollHeight;
  }, [lines, follow]);

  return (
    <div ref={ref} className={cn("rounded-lg border bg-muted/30 overflow-auto font-mono text-xs leading-5", className)}>
      {lines.length === 0 ? (
        <div className="p-6 text-center text-muted-foreground font-sans text-sm">No log lines.</div>
      ) : (
        <div className={cn("min-w-full", compact ? "py-0.5" : "py-1")}>
          {lines.map((line) => {
            const stream = streamName(line.stream);
            const sourceId = sourceIdOf(line);
            return (
              <div
                key={line.id ?? `${sourceId}-${line.sequence}`}
                className={cn(
                  "px-2 flex gap-2 items-baseline",
                  compact ? "py-0" : "py-0.5",
                  stream === "stderr" && "bg-red-500/5 text-red-700 dark:text-red-300",
                  wrap ? "whitespace-pre-wrap break-all" : "whitespace-pre",
                )}
              >
                {!compact && <span className="text-muted-foreground shrink-0 tabular-nums select-none">{formatTimestamp(line.timestamp, "HH:mm:ss.SSS")}</span>}
                {!compact && (
                  <span className="text-muted-foreground shrink-0 select-none truncate max-w-32" title={sourceId}>
                    {names.get(sourceId) ?? sourceId}
                  </span>
                )}
                <span className={cn("shrink-0 select-none w-11 text-[10px] uppercase", stream === "stderr" ? "text-red-600 dark:text-red-400" : "text-muted-foreground")}>{stream}</span>
                <span className="min-w-0 flex-1">
                  {stripAnsi(line.text)}
                  {line.truncated && <span className="text-muted-foreground"> [truncated]</span>}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
