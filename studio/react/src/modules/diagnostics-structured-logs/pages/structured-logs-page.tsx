"use client";

import { Pause, Play, RefreshCw } from "lucide-react";
import { useCallback, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ErrorAlert } from "@/components/common/error-alert";
import { Field } from "@/components/common/field";
import { PageHeader } from "@/components/common/page-header";
import { SearchInput } from "@/components/common/search-input";
import { SimpleSelect } from "@/components/common/simple-select";
import { ToneBadge } from "@/components/common/status-badge";
import { useUrlState } from "@/lib/utils/url-state";
import { StructuredLogLevels, droppedEvents, sourceLabel, sourceStatusName, type StructuredLogFilter, type StructuredLogLevel } from "../api";
import { StructuredLogTable } from "../components/log-table";
import { useRecentStructuredLogs, useStructuredLogSources } from "../hooks";

function toIso(local: string | undefined): string | undefined {
  if (!local) return undefined;
  const d = new Date(local);
  return Number.isNaN(d.getTime()) ? undefined : d.toISOString();
}

const takeOptions = ["100", "250", "500", "1000"].map((v) => ({ value: v, label: `${v} entries` }));

export function StructuredLogsPage() {
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
        title="Structured logs"
        description="Recent structured log events captured by the server."
        actions={
          <>
            <Button variant={follow ? "default" : "outline"} size="sm" onClick={() => toggleFollow(!follow)}>
              {follow ? <Pause data-icon="inline-start" /> : <Play data-icon="inline-start" />}
              {follow ? "Following" : "Follow"}
            </Button>
            <Button variant="outline" size="sm" onClick={() => recent.refetch()} disabled={recent.isFetching}>
              <RefreshCw data-icon="inline-start" className={recent.isFetching ? "animate-spin" : undefined} />
              Refresh
            </Button>
          </>
        }
      >
        <div className="flex flex-wrap items-end gap-2">
          <SearchInput value={text} onChange={onSearch} placeholder="Search message, properties, exception" className="w-72" />
          <SimpleSelect
            className="w-40"
            placeholder="Any level"
            allowEmpty
            value={level}
            options={StructuredLogLevels.map((l) => ({ value: l, label: `${l}+` }))}
            onChange={(v) => url.set({ level: v })}
          />
          <SimpleSelect
            className="w-56"
            placeholder="All sources"
            allowEmpty
            value={sourceId}
            options={sourceList.map((s) => ({ value: s.id, label: `${sourceLabel(s)}${sourceStatusName(s.status) !== "Connected" ? ` (${sourceStatusName(s.status).toLowerCase()})` : ""}` }))}
            onChange={(v) => url.set({ source: v })}
          />
          <Field label="Category prefix" htmlFor="sl-cat">
            <Input id="sl-cat" value={category} onChange={(e) => url.set({ category: e.target.value })} placeholder="Elsa.Workflows" className="w-52" />
          </Field>
          <Field label="Workflow instance" htmlFor="sl-inst">
            <Input id="sl-inst" value={instanceId} onChange={(e) => url.set({ instance: e.target.value })} className="w-52 font-mono text-xs" />
          </Field>
          <Field label="Trace id" htmlFor="sl-trace">
            <Input id="sl-trace" value={traceId} onChange={(e) => url.set({ trace: e.target.value })} className="w-52 font-mono text-xs" />
          </Field>
          <Field label="From" htmlFor="sl-from">
            <Input id="sl-from" type="datetime-local" value={from ?? ""} onChange={(e) => url.set({ from: e.target.value })} className="w-52" />
          </Field>
          <Field label="To" htmlFor="sl-to">
            <Input id="sl-to" type="datetime-local" value={to ?? ""} onChange={(e) => url.set({ to: e.target.value })} className="w-52" />
          </Field>
          <SimpleSelect className="w-36" value={String(take)} options={takeOptions} onChange={(v) => url.set({ take: v })} />
        </div>
      </PageHeader>

      <ErrorAlert error={recent.error} title="Could not load structured logs" />
      <ErrorAlert error={sources.error} title="Could not load log sources" />

      <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
        <span className="tabular-nums">{items.length} entries</span>
        {dropped > 0 && <ToneBadge tone="warning">{dropped} dropped</ToneBadge>}
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
