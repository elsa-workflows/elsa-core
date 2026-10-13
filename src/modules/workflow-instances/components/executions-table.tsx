"use client";

import { useState } from "react";
import { JsonViewer } from "@/components/common/json-viewer";
import { KeyValueTable } from "@/components/common/key-value-table";
import { StatusBadge } from "@/components/common/status-badge";
import { Timestamp } from "@/components/common/timestamp";
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { ActivityExecutionRecord } from "@/lib/api/types/workflow-instances";
import { elapsedMs, executionDisplayText, formatElapsed } from "../model";

function Section({ title, value }: { title: string; value: unknown }) {
  if (value === undefined || value === null) return null;
  if (typeof value === "object" && Object.keys(value as object).length === 0) return null;
  return (
    <div className="flex flex-col gap-1.5">
      <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{title}</h4>
      <JsonViewer value={value} maxHeight="18rem" />
    </div>
  );
}

/** Table of an activity's executions; clicking a row opens the record's details in a sheet. */
export function ExecutionsTable({ records }: { records: ActivityExecutionRecord[] }) {
  const [open, setOpen] = useState<ActivityExecutionRecord | null>(null);

  return (
    <>
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Status</TableHead>
              <TableHead>Started</TableHead>
              <TableHead>Completed</TableHead>
              <TableHead>Duration</TableHead>
              <TableHead>Exception</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {records.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-16 text-center text-muted-foreground">
                  This activity has not executed yet.
                </TableCell>
              </TableRow>
            ) : (
              records.map((r) => (
                <TableRow key={r.id} className="cursor-pointer" onClick={() => setOpen(r)}>
                  <TableCell>
                    <StatusBadge status={r.status} />
                  </TableCell>
                  <TableCell>
                    <Timestamp value={r.startedAt} />
                  </TableCell>
                  <TableCell>
                    <Timestamp value={r.completedAt} />
                  </TableCell>
                  <TableCell className="tabular-nums">{r.completedAt ? formatElapsed(elapsedMs(r.startedAt, r.completedAt)) : "–"}</TableCell>
                  <TableCell className="max-w-[24rem] truncate text-destructive">{r.exception?.message ?? ""}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <Sheet open={!!open} onOpenChange={(o) => !o && setOpen(null)}>
        <SheetContent className="sm:max-w-xl overflow-y-auto">
          {open && (
            <>
              <SheetHeader>
                <SheetTitle>{executionDisplayText(open)}</SheetTitle>
                <SheetDescription className="font-mono text-xs">{open.id}</SheetDescription>
              </SheetHeader>
              <div className="flex flex-col gap-4 px-4 pb-4">
                <KeyValueTable
                  rows={[
                    { label: "Status", value: <StatusBadge status={open.status} /> },
                    { label: "Activity type", value: <span className="font-mono text-xs">{open.activityType}</span> },
                    { label: "Node id", value: <span className="font-mono text-xs break-all">{open.activityNodeId}</span> },
                    { label: "Started", value: <Timestamp value={open.startedAt} /> },
                    { label: "Completed", value: <Timestamp value={open.completedAt} /> },
                    { label: "Has bookmarks", value: open.hasBookmarks ? "Yes" : "No" },
                  ]}
                />
                {open.exception && (
                  <div className="flex flex-col gap-1.5">
                    <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Exception</h4>
                    <div className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-xs">
                      <div className="font-mono">{open.exception.type}</div>
                      <div className="mt-1">{open.exception.message}</div>
                      {open.exception.stackTrace && <pre className="mt-2 max-h-64 overflow-auto whitespace-pre-wrap font-mono text-[11px]">{open.exception.stackTrace}</pre>}
                    </div>
                  </div>
                )}
                <Section title="Activity state" value={open.activityState} />
                <Section title="Outputs" value={open.outputs} />
                <Section title="Payload" value={open.payload} />
                <Section title="Properties" value={open.properties} />
              </div>
            </>
          )}
        </SheetContent>
      </Sheet>
    </>
  );
}
