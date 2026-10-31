"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { X } from "lucide-react";
import { useMemo, useState } from "react";
import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";
import { StatusBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@elsa-workflows/react-studio-ui/tabs";
import type { Activity } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { ActivityExecutionRecord, WorkflowExecutionLogRecord } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";
import { activityDisplayText, useDesigner } from "@elsa-workflows/react-studio-sdk/designer/index";
import { usePermission, Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { RetriesPanel } from "./retries-panel";
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
  const _t = useTranslation();
  const { registry } = useDesigner();
  const canViewRetries = usePermission(Resources.resilienceRetries, Verbs.view);
  const [tab, setTab] = useState("executions");
  const descriptor = registry.get(activity.type, activity.version);
  const showRetries = canViewRetries && (descriptor?.customProperties?.Resilient === true || activity.customProperties?.resilienceStrategy != null);
  const lastEvent = useMemo(() => [...journal].reverse().find((r) => r.activityId === activity.id), [journal, activity.id]);
  const latest = executions[executions.length - 1];

  return (
    <div className="flex h-full min-h-0 flex-col">
      <Tabs value={tab === "retries" && !showRetries ? "executions" : tab} onValueChange={(v) => setTab(String(v))} className="h-full min-h-0 gap-0">
        <div className="flex items-center gap-3 border-b px-3 py-1.5">
          <div className="flex min-w-0 items-center gap-2">
            <span className="truncate text-sm font-medium">{activityDisplayText(activity, descriptor)}</span>
            <span className="truncate text-xs text-muted-foreground">{descriptor?.displayName ?? activity.type}</span>
            {latest && <StatusBadge status={latest.status} className="h-4 px-1.5 text-[10px]" />}
          </div>
          <TabsList variant="line" className="ml-auto">
            <TabsTrigger value="event">{_t("Event")}</TabsTrigger>
            <TabsTrigger value="activity">{_t("Activity")}</TabsTrigger>
            <TabsTrigger value="executions">
              {_t("Executions")}{executions.length > 0 && <span className="rounded-full bg-muted px-1.5 text-[10px] tabular-nums">{executions.length}</span>}
            </TabsTrigger>
            {showRetries && <TabsTrigger value="retries">{_t("Retries")}</TabsTrigger>}
          </TabsList>
          <Button variant="ghost" size="icon-xs" aria-label={_t("Close")} onClick={onClose}>
            <X />
          </Button>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <TabsContent value="event" className="flex flex-col gap-3">
            {lastEvent ? (
              <>
                <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
                  <span>
                    {_t("Event")}{" "}<span className="font-medium text-foreground">{lastEvent.eventName}</span>
                  </span>
                  <span>{_t("Sequence")}{" "}{lastEvent.sequence}</span>
                  <span className="tabular-nums">{lastEvent.timestamp}</span>
                  {lastEvent.message && <span className="text-foreground">{lastEvent.message}</span>}
                </div>
                <JsonViewer value={{ payload: lastEvent.payload ?? null, activityState: lastEvent.activityState ?? null }} maxHeight="100%" />
              </>
            ) : (
              <p className="text-sm text-muted-foreground">{_t("No journal entry for this activity yet.")}</p>
            )}
          </TabsContent>
          <TabsContent value="activity">
            <JsonViewer value={activity} maxHeight="100%" />
          </TabsContent>
          {showRetries && <TabsContent value="retries"><RetriesPanel key={activity.id} executions={executions} /></TabsContent>}
          <TabsContent value="executions">
            <ExecutionsTable records={executions} />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}
