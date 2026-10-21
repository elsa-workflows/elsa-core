"use client";

import { ArrowDownToLine, ArrowUpFromLine } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { dataRefKey, revealActivity, useDesigner, useDesignerState, type DataUsage, type DataUse } from "@/modules/designer";

/**
 * "↑ 1 · ↓ 2" usage summary for a variable/input/output. Hovering highlights the activities on the
 * canvas; clicking opens the list of activities, and clicking one of those selects it.
 */
export function DataUsageChips({ usage }: { usage: DataUsage | undefined }) {
  const { store } = useDesigner();
  const highlighted = useDesignerState((s) => !!usage && s.dataHighlight === dataRefKey(usage));
  if (!usage) return <span className="text-xs text-muted-foreground">–</span>;
  const total = usage.writers.length + usage.readers.length;
  const key = dataRefKey(usage);
  const setHighlight = (on: boolean) => store.getState().setDataHighlight(on ? key : undefined);

  return (
    <Popover>
      <PopoverTrigger
        render={
          <button
            type="button"
            onMouseEnter={() => setHighlight(true)}
            onMouseLeave={() => setHighlight(false)}
            onClick={(e) => e.stopPropagation()}
            className={cn(
              "inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-medium transition-colors hover:border-primary",
              highlighted ? "border-primary bg-accent" : "bg-card",
              total === 0 && "text-muted-foreground",
            )}
            title={total === 0 ? "Not used by any activity yet" : "Show where this is used"}
          />
        }
      >
        <span className="inline-flex items-center gap-0.5 text-status-warning"><ArrowUpFromLine className="size-3" />{usage.writers.length}</span>
        <span className="text-muted-foreground">·</span>
        <span className="inline-flex items-center gap-0.5 text-status-info"><ArrowDownToLine className="size-3" />{usage.readers.length}</span>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80 p-0" onMouseEnter={() => setHighlight(true)} onMouseLeave={() => setHighlight(false)}>
        <div className="border-b px-3 py-2 text-sm font-semibold">
          {usage.name} <span className="font-normal text-muted-foreground">· {usage.kind}</span>
        </div>
        {total === 0 ? (
          <p className="px-3 py-3 text-sm text-muted-foreground">Not referenced by any activity yet. Bind an activity output to it or use it in an expression.</p>
        ) : (
          <div className="max-h-72 overflow-y-auto py-1">
            <UseList title="Written by" uses={usage.writers} tone="text-status-warning" icon={<ArrowUpFromLine className="size-3.5" />} onPick={(id) => revealActivity(store, id)} />
            <UseList title="Read by" uses={usage.readers} tone="text-status-info" icon={<ArrowDownToLine className="size-3.5" />} onPick={(id) => revealActivity(store, id)} />
          </div>
        )}
      </PopoverContent>
    </Popover>
  );
}

function UseList({ title, uses, tone, icon, onPick }: { title: string; uses: DataUse[]; tone: string; icon: React.ReactNode; onPick: (activityId: string) => void }) {
  if (uses.length === 0) return null;
  return (
    <div>
      <div className={cn("eyebrow flex items-center gap-1 px-3 pt-2 pb-1", tone)}>
        {icon} {title}
      </div>
      {uses.map((u) => (
        <button key={`${u.activityId}:${u.property}`} type="button" onClick={() => onPick(u.activityId)} className="flex w-full items-center justify-between gap-2 px-3 py-1.5 text-left text-sm hover:bg-accent">
          <span className="truncate font-medium">{u.activityName}</span>
          <span className="shrink-0 text-xs text-muted-foreground">
            {u.property} · {u.via === "binding" ? "output binding" : u.via}
          </span>
        </button>
      ))}
    </div>
  );
}
