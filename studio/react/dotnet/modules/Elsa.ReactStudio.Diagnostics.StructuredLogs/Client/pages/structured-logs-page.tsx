"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Pause, Play, RefreshCw } from "lucide-react";
import { useCallback, useState } from "react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { SearchInput } from "@elsa-workflows/react-studio-ui/common/search-input";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { useUrlState } from "@elsa-workflows/react-studio-sdk/utils/url-state";
import { StructuredLogLevels, droppedEvents, sourceLabel, sourceStatusName, type StructuredLogFilter, type StructuredLogLevel } from "../api";
import { StructuredLogTable } from "../components/log-table";
import { useRecentStructuredLogs, useStructuredLogSources } from "../hooks";

function toIso(local: string | undefined): string | undefined {
  if (!local) return undefined;
  const d = new Date(local);
  return Number.isNaN(d.getTime()) ? undefined : d.toISOString();
}


export function StructuredLogsPage() {
  const _t = useTranslation();
  const takeOptions = ["100", "250", "500", "1000"].map((v) => ({ value: v, label: _t("{{value0}} entries", { value0: v }) }));
  const url = useUrlState();
  const level = url.get("level") as StructuredLogLevel | undefined;
  const sourceId = url.get("source");
  const category = url.get("category") ?? "";
  const text = url.get("q") ?? "";
  const instanceId = url.get("instance") ?? "";
  const traceId = url.get("trace") ?? "";
  const from = url.get("from");
  const to = url.get("to");
  const take = url.getNumber("take", 250);
  const [follow, setFollow] = useState(url.get("follow") === "1");

  const sources = useStructuredLogSources();
  const filter: StructuredLogFilter = {
    minimumLevel: level,
    sourceId: sourceId || undefined,
    categoryPrefix: category || undefined,
    text: text || undefined,
    workflowInstanceId: instanceId || undefined,
    traceId: traceId || undefined,
    from: toIso(from),
    to: toIso(to),
    take,
  };
  const recent = useRecentStructuredLogs(filter, follow);
  const onSearch = useCallback((q: string) => url.set({ q }), [url]);
  const toggleFollow = (v: boolean) => {
    setFollow(v);
    url.set({ follow: v ? "1" : undefined });
  };

  const items = recent.data?.items ?? [];
  const dropped = droppedEvents(recent.data);
  const sourceList = sources.data ?? [];

  return (
    <div className="flex flex-col gap-3">
      <PageHeader
        title={_t("Structured logs")}
        description={_t("Recent structured log events captured by the server.")}
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
          <SearchInput value={text} onChange={onSearch} placeholder={_t("Search message, properties, exception")} className="w-72" />
          <SimpleSelect
            className="w-40"
            placeholder={_t("Any level")}
            allowEmpty
            value={level}
            options={StructuredLogLevels.map((l) => ({ value: l, label: _t("{{value0}}+", { value0: l }) }))}
            onChange={(v) => url.set({ level: v })}
          />
          <SimpleSelect
            className="w-56"
            placeholder={_t("All sources")}
            allowEmpty
            value={sourceId}
            options={sourceList.map((s) => ({ value: s.id, label: _t("{{value0}}{{value1}}", { value0: sourceLabel(s), value1: sourceStatusName(s.status) !== "Connected" ? ` (${sourceStatusName(s.status).toLowerCase()})` : "" }) }))}
            onChange={(v) => url.set({ source: v })}
          />
          <Field label={_t("Category prefix")} htmlFor="sl-cat">
            <Input id="sl-cat" value={category} onChange={(e) => url.set({ category: e.target.value })} placeholder={_t("Elsa.Workflows")} className="w-52" />
          </Field>
          <Field label={_t("Workflow instance")} htmlFor="sl-inst">
            <Input id="sl-inst" value={instanceId} onChange={(e) => url.set({ instance: e.target.value })} className="w-52 font-mono text-xs" />
          </Field>
          <Field label={_t("Trace id")} htmlFor="sl-trace">
            <Input id="sl-trace" value={traceId} onChange={(e) => url.set({ trace: e.target.value })} className="w-52 font-mono text-xs" />
          </Field>
          <Field label={_t("From")} htmlFor="sl-from">
            <Input id="sl-from" type="datetime-local" value={from ?? ""} onChange={(e) => url.set({ from: e.target.value })} className="w-52" />
          </Field>
          <Field label={_t("To")} htmlFor="sl-to">
            <Input id="sl-to" type="datetime-local" value={to ?? ""} onChange={(e) => url.set({ to: e.target.value })} className="w-52" />
          </Field>
          <SimpleSelect className="w-36" value={String(take)} options={takeOptions} onChange={(v) => url.set({ take: v })} />
        </div>
      </PageHeader>

      <ErrorAlert error={recent.error} title={_t("Could not load structured logs")} />
      <ErrorAlert error={sources.error} title={_t("Could not load log sources")} />

      <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
        <span className="tabular-nums">{items.length} {_t("entries")}</span>
        {dropped > 0 && <ToneBadge tone="warning">{dropped} {_t("dropped")}</ToneBadge>}
        {sourceList.map((s) => (
          <ToneBadge key={s.id} tone={sourceStatusName(s.status) === "Connected" ? "success" : sourceStatusName(s.status) === "Stale" ? "warning" : "neutral"}>
            {sourceLabel(s)}
          </ToneBadge>
        ))}
      </div>

      <StructuredLogTable items={items} isLoading={recent.isLoading} />
    </div>
  );
}
