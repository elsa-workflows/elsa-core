"use client";

import { AlertTriangle, ChevronRight } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { Timestamp } from "@/components/common/timestamp";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import type { WorkflowIncident } from "@/lib/api/types/workflow-instances";

function stackOf(incident: WorkflowIncident) {
  return incident.exception?.stackTrace;
}

export function IncidentsPanel({ incidents, onSelectActivity }: { incidents: WorkflowIncident[]; onSelectActivity?: (activityId: string) => void }) {
  if (incidents.length === 0) return <EmptyState title="No incidents" description="This instance has not faulted." className="m-3 p-6" />;
  return (
    <ul className="flex flex-col gap-2 p-3">
      {incidents.map((incident, i) => {
        const stack = stackOf(incident);
        return (
          <li key={`${incident.activityId}-${incident.timestamp}-${i}`} className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm">
            <div className="flex items-start gap-2">
              <AlertTriangle className="mt-0.5 size-4 shrink-0 text-destructive" />
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center justify-between gap-1">
                  <button type="button" className="font-medium hover:underline" onClick={() => onSelectActivity?.(incident.activityId)} title={incident.activityType}>
                    {incident.activityType.split(".").pop()}
                  </button>
                  <span className="text-xs text-muted-foreground">
                    <Timestamp value={incident.timestamp} />
                  </span>
                </div>
                <p className="mt-1 break-words">{incident.message}</p>
                {incident.exception && (
                  <div className="mt-2 text-xs">
                    <div className="font-mono text-muted-foreground">{incident.exception.type}</div>
                    {incident.exception.message !== incident.message && <div className="mt-0.5 break-words">{incident.exception.message}</div>}
                  </div>
                )}
                {stack && (
                  <Collapsible className="mt-2">
                    <CollapsibleTrigger className="group flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
                      <ChevronRight className="size-3 transition-transform group-data-panel-open:rotate-90" />
                      Stack trace
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                      <pre className="mt-1 max-h-64 overflow-auto whitespace-pre-wrap rounded bg-muted/60 p-2 font-mono text-[11px]">{stack}</pre>
                    </CollapsibleContent>
                  </Collapsible>
                )}
              </div>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
