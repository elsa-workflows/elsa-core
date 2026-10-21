"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { AlertTriangle, Loader2, RefreshCw } from "lucide-react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import type { CallContextState } from "./use-call-context";

/** Floating status for the call-context view: loading, counts, empty sides, limits and errors. */
export function CallContextStatus({ state }: { state: CallContextState }) {
  const _t = useTranslation();
  if (state.unsupported) {
    return (
      <Pill tone="muted">{_t("Call context is shown on the workflow's top-level flowchart. Go back to the root to see callers and callees.")}</Pill>
    );
  }
  if (state.status === "error") {
    return (
      <Pill tone="danger">
        <AlertTriangle className="size-3.5" />
        {_t("Could not load the call graph:")}{" "}{describeError(state.error)}
        <Button variant="ghost" size="xs" onClick={state.refresh}>
          <RefreshCw /> {_t("Retry")}</Button>
      </Pill>
    );
  }
  const loading = state.status === "loading";
  const { incoming, outgoing } = state.counts;
  const parts: string[] = [];
  parts.push(incoming === 0 ? (loading ? translate("finding callers…") : translate("no callers")) : `${incoming} caller${incoming === 1 ? "" : "s"}`);
  parts.push(outgoing === 0 ? (loading ? translate("finding callees…") : translate("no callees")) : `${outgoing} callee${outgoing === 1 ? "" : "s"}`);
  const limits = state.warnings.filter((w) => w.code !== "load-failed");
  return (
    <Pill tone={limits.length || state.truncated.incoming || state.truncated.outgoing ? "warning" : "muted"}>
      {loading && <Loader2 className="size-3.5 animate-spin" />}
      <span>{parts.join(" · ")}</span>
      {(state.truncated.incoming || state.truncated.outgoing) && <span>{_t("· some branches not shown (limit)")}</span>}
      {limits.map((w) => (
        <span key={w.code} title={w.message}>
          · {w.code === "timeout" ? _t("timed out") : w.code === "max-requests" ? _t("request limit") : _t("node limit")}
        </span>
      ))}
      {!loading && (
        <Button variant="ghost" size="icon-xs" aria-label={_t("Rebuild call graph")} title={_t("Rebuild call graph")} onClick={state.refresh}>
          <RefreshCw />
        </Button>
      )}
    </Pill>
  );
}

function Pill({ tone, children }: { tone: "muted" | "warning" | "danger"; children: React.ReactNode }) {
  useTranslation();
  const cls = tone === "danger" ? "border-status-danger/40 text-status-danger" : tone === "warning" ? "border-status-warning/40 text-foreground" : "text-muted-foreground";
  return (
    <div className={`pointer-events-auto absolute bottom-3 left-1/2 z-10 flex -translate-x-1/2 items-center gap-1.5 rounded-full border bg-card/95 px-3 py-1 text-xs shadow-sm backdrop-blur ${cls}`}>
      {children}
    </div>
  );
}
