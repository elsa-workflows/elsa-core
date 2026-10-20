"use client";

import { X } from "lucide-react";
import { useMemo, useState } from "react";
import { JsonViewer } from "@/components/common/json-viewer";
import { StatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type { Activity } from "@/lib/api/types/activities";
import type { ActivityExecutionRecord, WorkflowExecutionLogRecord } from "@/lib/api/types/workflow-instances";
import { activityDisplayText, useDesigner } from "@/modules/designer";
import { ExecutionsTable } from "./executions-table";

/** Bottom pane of the viewer: what happened to the selected activity. */
export function ActivityDetailsPane({
  activity,
  journal,
  executions,
  onClose,
}: {
  activity: Activity;
  journal: WorkflowExecutionLogRecord[];
  executions: ActivityExecutionRecord[];
  onClose: () => void;
}) {
  const { registry } = useDesigner();
  const [tab, setTab] = useState("executions");
  const descriptor = registry.get(activity.type, activity.version);
  const lastEvent = useMemo(() => [...journal].reverse().find((r) => r.activityId === activity.id), [journal, activity.id]);
  const latest = executions[executions.length - 1];

  return (
    <div className="flex h-full min-h-0 flex-col">
      <Tabs value={tab} onValueChange={(v) => setTab(String(v))} className="h-full min-h-0 gap-0">
        <div className="flex items-center gap-3 border-b px-3 py-1.5">
          <div className="flex min-w-0 items-center gap-2">
            <span className="truncate text-sm font-medium">{activityDisplayText(activity, descriptor)}</span>
            <span className="truncate text-xs text-muted-foreground">{descriptor?.displayName ?? activity.type}</span>
            {latest && <StatusBadge status={latest.status} className="h-4 px-1.5 text-[10px]" />}
          </div>
          <TabsList variant="line" className="ml-auto">
            <TabsTrigger value="event">Event</TabsTrigger>
            <TabsTrigger value="activity">Activity</TabsTrigger>
            <TabsTrigger value="executions">
              Executions
              {executions.length > 0 && <span className="rounded-full bg-muted px-1.5 text-[10px] tabular-nums">{executions.length}</span>}
            </TabsTrigger>
          </TabsList>
          <Button variant="ghost" size="icon-xs" aria-label="Close" onClick={onClose}>
            <X />
          </Button>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <TabsContent value="event" className="flex flex-col gap-3">
            {lastEvent ? (
              <>
                <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
                  <span>
                    Event <span className="font-medium text-foreground">{lastEvent.eventName}</span>
                  </span>
                  <span>Sequence {lastEvent.sequence}</span>
                  <span className="tabular-nums">{lastEvent.timestamp}</span>
                  {lastEvent.message && <span className="text-foreground">{lastEvent.message}</span>}
                </div>
                <JsonViewer value={{ payload: lastEvent.payload ?? null, activityState: lastEvent.activityState ?? null }} maxHeight="100%" />
              </>
            ) : (
              <p className="text-sm text-muted-foreground">No journal entry for this activity yet.</p>
            )}
          </TabsContent>
          <TabsContent value="activity">
            <JsonViewer value={activity} maxHeight="100%" />
          </TabsContent>
          <TabsContent value="executions">
            <ExecutionsTable records={executions} />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}
