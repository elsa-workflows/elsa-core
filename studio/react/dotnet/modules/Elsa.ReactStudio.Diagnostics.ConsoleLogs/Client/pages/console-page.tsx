"use client";
import { translate, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Pause, Play, RefreshCw } from "lucide-react";
import { useCallback, useState } from "react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Switch } from "@elsa-workflows/react-studio-ui/switch";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { SearchInput } from "@elsa-workflows/react-studio-ui/common/search-input";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { useUrlState } from "@elsa-workflows/react-studio-sdk/utils/url-state";
import { droppedCount, healthName, type ConsoleLogFilter, type ConsoleStream } from "../api";
import { ConsoleLogView } from "../components/console-log-view";
import { useConsoleLogSources, useRecentConsoleLogs, useViewPreference } from "../hooks";

function toIso(local: string | undefined): string | undefined {
  if (!local) return undefined;
  const d = new Date(local);
  return Number.isNaN(d.getTime()) ? undefined : d.toISOString();
}

const limitOptions = ["100", "250", "500", "1000", "2000"].map((v) => ({ value: v, label: translate("{{value0}} lines", { value0: v }) }));

export function ConsoleLogsPage() {
  const _t = useTranslation();
  const url = useUrlState();
  const sourceId = url.get("source");
  const stream = url.get("stream") as ConsoleStream | undefined;
  const text = url.get("q") ?? "";
  const from = url.get("from");
  const to = url.get("to");
  const limit = url.getNumber("limit", 500);
  const [follow, setFollow] = useState(url.get("follow") === "1");
  const [wrap, setWrap] = useViewPreference("wrap", true);
  const [compact, setCompact] = useViewPreference("compact", false);

  const sources = useConsoleLogSources();
  const filter: ConsoleLogFilter = {
    sourceId: sourceId || undefined,
    stream: stream ?? null,
    query: text || undefined,
    from: toIso(from),
    to: toIso(to),
    limit,
  };
  const recent = useRecentConsoleLogs(filter, follow);
  const onSearch = useCallback((q: string) => url.set({ q }), [url]);
  const toggleFollow = (v: boolean) => {
    setFollow(v);
    url.set({ follow: v ? "1" : undefined });
  };

  const lines = recent.data?.items ?? [];
  const dropped = droppedCount(recent.data);
  const sourceList = sources.data ?? recent.data?.sources ?? [];

  return (
    <div className="flex flex-col gap-3 h-full">
      <PageHeader
        title={_t("Console")}
        description={_t("Recent stdout/stderr lines captured from the server.")}
        actions={
          <>
            <Button variant={follow ? "default" : "outline"} size="sm" onClick={() => toggleFollow(!follow)}>
              {follow ? <Pause data-icon="inline-start" /> : <Play data-icon="inline-start" />}
              {follow ? _t("Following") : _t("Follow")}
            </Button>
            <Button variant="outline" size="sm" onClick={() => recent.refetch()} disabled={recent.isFetching}>
              <RefreshCw data-icon="inline-start" className={recent.isFetching ? "animate-spin" : undefined} />
              {_t("Refresh")}</Button>
          </>
        }
      >
        <div className="flex flex-wrap items-end gap-2">
          <SearchInput value={text} onChange={onSearch} placeholder={_t("Filter text")} className="w-72" />
          <SimpleSelect
            className="w-56"
            placeholder={_t("All sources")}
            allowEmpty
            value={sourceId}
            options={sourceList.map((s) => ({ value: s.id, label: _t("{{value0}}{{value1}}", { value0: s.displayName, value1: healthName(s.health) !== "Connected" ? ` (${healthName(s.health).toLowerCase()})` : "" }) }))}
            onChange={(v) => url.set({ source: v })}
          />
          <SimpleSelect
            className="w-32"
            placeholder={_t("Both streams")}
            allowEmpty
            value={stream}
            options={[
              { value: "stdout", label: _t("stdout") },
              { value: "stderr", label: _t("stderr") },
            ]}
            onChange={(v) => url.set({ stream: v })}
          />
          <Field label={_t("From")} htmlFor="cl-from">
            <Input id="cl-from" type="datetime-local" value={from ?? ""} onChange={(e) => url.set({ from: e.target.value })} className="w-52" />
          </Field>
          <Field label={_t("To")} htmlFor="cl-to">
            <Input id="cl-to" type="datetime-local" value={to ?? ""} onChange={(e) => url.set({ to: e.target.value })} className="w-52" />
          </Field>
          <SimpleSelect className="w-32" value={String(limit)} options={limitOptions} onChange={(v) => url.set({ limit: v })} />
          <Field inline label={_t("Wrap")} htmlFor="cl-wrap">
            <Switch id="cl-wrap" checked={wrap} onCheckedChange={setWrap} />
          </Field>
          <Field inline label={_t("Compact")} htmlFor="cl-compact">
            <Switch id="cl-compact" checked={compact} onCheckedChange={setCompact} />
          </Field>
        </div>
      </PageHeader>

      <ErrorAlert error={recent.error} title={_t("Could not load console logs")} />
      <ErrorAlert error={sources.error} title={_t("Could not load log sources")} />

      <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
        <span className="tabular-nums">{lines.length} {_t("lines")}</span>
        {dropped > 0 && <ToneBadge tone="warning">{dropped} {_t("dropped")}</ToneBadge>}
        {sourceList.map((s) => (
          <ToneBadge key={s.id} tone={healthName(s.health) === "Connected" ? "success" : healthName(s.health) === "Stale" ? "warning" : "neutral"}>
            {s.displayName}
          </ToneBadge>
        ))}
      </div>

      <ConsoleLogView lines={lines} sources={sourceList} wrap={wrap} compact={compact} follow={follow} className="min-h-[24rem] max-h-[calc(100vh-22rem)]" />
    </div>
  );
}
