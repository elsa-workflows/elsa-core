"use client";

import { Fragment } from "react";
import { ToneBadge } from "@/components/common/status-badge";
import { Timestamp } from "@/components/common/timestamp";
import { cn } from "@/lib/utils";
import { isErrorLevel, logLevelName, type AlterationLogEntry, type RunAlterationsResult } from "../api";

function toneFor(level: AlterationLogEntry["logLevel"]): "neutral" | "warning" | "danger" {
  const name = logLevelName(level);
  if (isErrorLevel(level)) return "danger";
  if (name === "Warning") return "warning";
  return "neutral";
}

export function AlterationLog({ entries, className }: { entries: AlterationLogEntry[] | null | undefined; className?: string }) {
  if (!entries?.length) return <p className={cn("text-xs text-muted-foreground", className)}>No log entries.</p>;
  return (
    <div className={cn("grid grid-cols-[auto_auto_1fr] gap-x-3 gap-y-1 text-xs", className)}>
      {entries.map((e, i) => (
        <Fragment key={i}>
          <span className="text-muted-foreground whitespace-nowrap">
            <Timestamp value={e.timestamp} />
          </span>
          <ToneBadge tone={toneFor(e.logLevel)} className="text-[10px] px-1.5 py-0">
            {logLevelName(e.logLevel)}
          </ToneBadge>
          <span className="break-words">
            {e.eventName && <span className="font-medium mr-1">{e.eventName}:</span>}
            {e.message}
          </span>
        </Fragment>
      ))}
    </div>
  );
}

/** Renders the per-instance outcome of `POST /alterations/run` or `/alterations/workflows/retry`. */
export function AlterationResults({ results }: { results: RunAlterationsResult[] }) {
  if (results.length === 0) return <p className="text-sm text-muted-foreground">No workflow instances matched; nothing was altered.</p>;
  return (
    <div className="flex flex-col gap-3">
      {results.map((r) => (
        <div key={r.workflowInstanceId} className="rounded-lg border p-3 flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-xs truncate">{r.workflowInstanceId}</span>
            <ToneBadge tone={r.isSuccessful ? "success" : "danger"}>{r.isSuccessful ? "Succeeded" : "Failed"}</ToneBadge>
            {r.workflowHasScheduledWork && <ToneBadge tone="info">Scheduled work dispatched</ToneBadge>}
          </div>
          <AlterationLog entries={r.log?.logEntries} />
        </div>
      ))}
    </div>
  );
}
