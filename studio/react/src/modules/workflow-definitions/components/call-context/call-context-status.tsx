"use client";

import { AlertTriangle, Loader2, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { describeError } from "@/components/common/error-alert";
import type { CallContextState } from "./use-call-context";

/** Floating status for the call-context view: loading, counts, empty sides, limits and errors. */
export function CallContextStatus({ state }: { state: CallContextState }) {
  if (state.unsupported) {
    return (
      <Pill tone="muted">Call context is shown on the workflow&apos;s top-level flowchart. Go back to the root to see callers and callees.</Pill>
    );
  }
  if (state.status === "error") {
    return (
      <Pill tone="danger">
        <AlertTriangle className="size-3.5" />
        Could not load the call graph: {describeError(state.error)}
        <Button variant="ghost" size="xs" onClick={state.refresh}>
          <RefreshCw /> Retry
        </Button>
      </Pill>
    );
  }
  const loading = state.status === "loading";
  const { incoming, outgoing } = state.counts;
  const parts: string[] = [];
  parts.push(incoming === 0 ? (loading ? "finding callers…" : "no callers") : `${incoming} caller${incoming === 1 ? "" : "s"}`);
  parts.push(outgoing === 0 ? (loading ? "finding callees…" : "no callees") : `${outgoing} callee${outgoing === 1 ? "" : "s"}`);
  const limits = state.warnings.filter((w) => w.code !== "load-failed");
  return (
    <Pill tone={limits.length || state.truncated.incoming || state.truncated.outgoing ? "warning" : "muted"}>
      {loading && <Loader2 className="size-3.5 animate-spin" />}
      <span>{parts.join(" · ")}</span>
      {(state.truncated.incoming || state.truncated.outgoing) && <span>· some branches not shown (limit)</span>}
      {limits.map((w) => (
        <span key={w.code} title={w.message}>
          · {w.code === "timeout" ? "timed out" : w.code === "max-requests" ? "request limit" : "node limit"}
        </span>
      ))}
      {!loading && (
        <Button variant="ghost" size="icon-xs" aria-label="Rebuild call graph" title="Rebuild call graph" onClick={state.refresh}>
          <RefreshCw />
        </Button>
      )}
    </Pill>
  );
}

function Pill({ tone, children }: { tone: "muted" | "warning" | "danger"; children: React.ReactNode }) {
  const cls = tone === "danger" ? "border-status-danger/40 text-status-danger" : tone === "warning" ? "border-status-warning/40 text-foreground" : "text-muted-foreground";
  return (
    <div className={`pointer-events-auto absolute bottom-3 left-1/2 z-10 flex -translate-x-1/2 items-center gap-1.5 rounded-full border bg-card/95 px-3 py-1 text-xs shadow-sm backdrop-blur ${cls}`}>
      {children}
    </div>
  );
}
