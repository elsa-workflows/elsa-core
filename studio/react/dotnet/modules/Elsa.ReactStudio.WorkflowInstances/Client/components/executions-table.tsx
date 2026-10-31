"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useState } from "react";
import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";
import { KeyValueTable } from "@elsa-workflows/react-studio-ui/common/key-value-table";
import { StatusBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "@elsa-workflows/react-studio-ui/sheet";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@elsa-workflows/react-studio-ui/table";
import type { ActivityExecutionRecord } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";
import { elapsedMs, executionDisplayText, formatElapsed } from "../model";

function Section({ title, value }: { title: string; value: unknown }) {
  useTranslation();
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
  const _t = useTranslation();
  const [open, setOpen] = useState<ActivityExecutionRecord | null>(null);

  return (
    <>
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{_t("Status")}</TableHead>
              <TableHead>{_t("Started")}</TableHead>
              <TableHead>{_t("Completed")}</TableHead>
              <TableHead>{_t("Duration")}</TableHead>
              <TableHead>{_t("Exception")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {records.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-16 text-center text-muted-foreground">
                  {_t("This activity has not executed yet.")}</TableCell>
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
                    { label: _t("Status"), value: <StatusBadge status={open.status} /> },
                    { label: _t("Activity type"), value: <span className="font-mono text-xs">{open.activityType}</span> },
                    { label: _t("Node id"), value: <span className="font-mono text-xs break-all">{open.activityNodeId}</span> },
                    { label: _t("Started"), value: <Timestamp value={open.startedAt} /> },
                    { label: _t("Completed"), value: <Timestamp value={open.completedAt} /> },
                    { label: _t("Has bookmarks"), value: open.hasBookmarks ? "Yes" : "No" },
                  ]}
                />
                {open.exception && (
                  <div className="flex flex-col gap-1.5">
                    <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{_t("Exception")}</h4>
                    <div className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-xs">
                      <div className="font-mono">{open.exception.type}</div>
                      <div className="mt-1">{open.exception.message}</div>
                      {open.exception.stackTrace && <pre className="mt-2 max-h-64 overflow-auto whitespace-pre-wrap font-mono text-[11px]">{open.exception.stackTrace}</pre>}
                    </div>
                  </div>
                )}
                <Section title={_t("Activity state")} value={open.activityState} />
                <Section title={_t("Outputs")} value={open.outputs} />
                <Section title={_t("Payload")} value={open.payload} />
                <Section title={_t("Properties")} value={open.properties} />
              </div>
            </>
          )}
        </SheetContent>
      </Sheet>
    </>
  );
}
