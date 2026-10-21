"use client";

import { ChevronDown, ChevronRight, Loader2 } from "lucide-react";
import { Fragment, useState } from "react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { KeyValueTable } from "@/components/common/key-value-table";
import { CopyButton } from "@/components/common/copy-button";
import { formatTimestamp } from "@/components/common/timestamp";
import { cn } from "@/lib/utils";
import { levelName, type StructuredLogEvent } from "../api";
import { LevelBadge } from "./level-badge";

function entries(map: Record<string, string | null> | null | undefined) {
  return Object.entries(map ?? {});
}

function MapBlock({ title, map }: { title: string; map: Record<string, string | null> | null | undefined }) {
  const rows = entries(map);
  if (rows.length === 0) return null;
  return (
    <div className="flex flex-col gap-1">
      <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">{title}</h4>
      <KeyValueTable rows={rows.map(([k, v]) => ({ label: <span className="font-mono text-xs">{k}</span>, value: <span className="font-mono text-xs break-all">{v ?? ""}</span> }))} />
    </div>
  );
}

function Id({ label, value }: { label: string; value: string | null | undefined }) {
  if (!value) return null;
  return (
    <span className="inline-flex items-center gap-1 text-xs">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-mono">{value}</span>
      <CopyButton value={value} label={`Copy ${label}`} />
    </span>
  );
}

/** Table of structured log entries with an expandable detail row per entry. */
export function StructuredLogTable({ items, isLoading, className }: { items: StructuredLogEvent[]; isLoading?: boolean; className?: string }) {
  const [open, setOpen] = useState<Set<string>>(() => new Set());
  const toggle = (id: string) =>
    setOpen((s) => {
      const next = new Set(s);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  return (
    <div className={cn("rounded-lg border overflow-x-auto", className)}>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-6" />
            <TableHead className="w-40">Timestamp</TableHead>
            <TableHead className="w-14">Level</TableHead>
            <TableHead className="w-64">Source</TableHead>
            <TableHead>Message</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading && items.length === 0 ? (
            <TableRow>
              <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                <Loader2 className="inline size-4 animate-spin mr-2" />
                Loading
              </TableCell>
            </TableRow>
          ) : items.length === 0 ? (
            <TableRow>
              <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                No log entries match the current filters.
              </TableCell>
            </TableRow>
          ) : (
            items.map((e) => {
              const expanded = open.has(e.id);
              const isError = levelName(e.level) === "Error" || levelName(e.level) === "Critical";
              return (
                <Fragment key={e.id}>
                  <TableRow className={cn("cursor-pointer", isError && "bg-red-500/5")} onClick={() => toggle(e.id)} data-state={expanded ? "selected" : undefined}>
                    <TableCell className="text-muted-foreground">{expanded ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}</TableCell>
                    <TableCell className="font-mono text-xs tabular-nums whitespace-nowrap">{formatTimestamp(e.timestamp, "yyyy-MM-dd HH:mm:ss.SSS")}</TableCell>
                    <TableCell>
                      <LevelBadge level={e.level} />
                    </TableCell>
                    <TableCell className="text-xs">
                      <div className="truncate max-w-64" title={e.category}>
                        {e.category}
                      </div>
                      {e.eventName && <div className="text-muted-foreground truncate max-w-64">{e.eventName}</div>}
                    </TableCell>
                    <TableCell className={cn("text-xs", !expanded && "truncate max-w-xl")}>
                      <span className={cn(expanded && "whitespace-pre-wrap break-words")}>{e.message}</span>
                      {e.exception && !expanded && <span className="ml-2 text-destructive">{e.exception.type}: {e.exception.message}</span>}
                    </TableCell>
                  </TableRow>
                  {expanded && (
                    <TableRow className="hover:bg-transparent">
                      <TableCell colSpan={5} className="bg-muted/30 p-4">
                        <div className="flex flex-col gap-4 text-sm">
                          <div className="flex flex-wrap gap-x-5 gap-y-1">
                            <Id label="trace" value={e.traceId} />
                            <Id label="span" value={e.spanId} />
                            <Id label="correlation" value={e.correlationId} />
                            <Id label="instance" value={e.workflowInstanceId} />
                            <Id label="definition" value={e.workflowDefinitionId} />
                            <Id label="tenant" value={e.tenantId} />
                            <Id label="source" value={e.sourceId} />
                            {typeof e.eventId === "number" && e.eventId !== 0 && <Id label="event id" value={String(e.eventId)} />}
                            <Id label="seq" value={String(e.sequence)} />
                          </div>
                          {e.messageTemplate && e.messageTemplate !== e.message && (
                            <div className="flex flex-col gap-1">
                              <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Template</h4>
                              <code className="font-mono text-xs break-all">{e.messageTemplate}</code>
                            </div>
                          )}
                          <MapBlock title="Properties" map={e.properties} />
                          <MapBlock title="Scopes" map={e.scopes} />
                          {e.exception && (
                            <div className="flex flex-col gap-1">
                              <h4 className="text-xs font-semibold text-destructive uppercase tracking-wide">Exception</h4>
                              <div className="text-xs">
                                <span className="font-mono">{e.exception.type}</span>: {e.exception.message}
                              </div>
                              {e.exception.stackTrace && (
                                <pre className="rounded-md border bg-background p-2 text-[11px] font-mono overflow-auto max-h-64 whitespace-pre-wrap break-all">{e.exception.stackTrace}</pre>
                              )}
                            </div>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  )}
                </Fragment>
              );
            })
          )}
        </TableBody>
      </Table>
    </div>
  );
}
