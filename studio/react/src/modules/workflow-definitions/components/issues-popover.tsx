"use client";

import { CircleCheck, Info, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { revealActivity, useDesigner, useDesignerState, validateWorkflow } from "@/modules/designer";
import { useMemo } from "react";

/** Live list of client-side findings for the current graph; clicking one selects the activity. */
export function IssuesPopover() {
  const { store, registry } = useDesigner();
  const root = useDesignerState((s) => s.root);
  const issues = useMemo(() => validateWorkflow(root, registry), [root, registry]);
  const warnings = issues.filter((i) => i.severity === "warning").length;

  return (
    <Popover>
      <PopoverTrigger
        render={
          <Button
            variant="ghost"
            size="sm"
            className={cn("gap-1.5", warnings ? "text-status-warning" : issues.length ? "text-muted-foreground" : "text-status-success")}
            aria-label="Issues"
          />
        }
      >
        {warnings ? <TriangleAlert /> : issues.length ? <Info /> : <CircleCheck />}
        {warnings ? `${warnings} warning${warnings === 1 ? "" : "s"}` : issues.length ? `${issues.length} hint${issues.length === 1 ? "" : "s"}` : "No issues"}
      </PopoverTrigger>
      <PopoverContent align="end" className="w-96 p-0">
        <div className="border-b px-3 py-2 text-sm font-semibold">Checks</div>
        <div className="max-h-80 overflow-y-auto">
          {issues.length === 0 && <p className="px-3 py-4 text-sm text-muted-foreground">Everything is connected. The server runs deeper validation when you save or publish.</p>}
          {issues.map((i) => (
            <button
              key={i.id}
              type="button"
              disabled={!i.activityId}
              onClick={() => i.activityId && revealActivity(store, i.activityId)}
              className="flex w-full items-start gap-2 px-3 py-2 text-left text-sm hover:bg-accent disabled:cursor-default disabled:hover:bg-transparent"
            >
              {i.severity === "warning" ? <TriangleAlert className="mt-0.5 size-4 shrink-0 text-status-warning" /> : <Info className="mt-0.5 size-4 shrink-0 text-status-info" />}
              <span>{i.message}</span>
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
