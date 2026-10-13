"use client";

import type { ReactNode } from "react";
import { ChevronRight, TriangleAlert } from "lucide-react";
import { cn } from "@/lib/utils";
import type { Activity } from "@/lib/api/types/activities";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";
import type { ActivityExecutionStats } from "@/lib/api/types/workflow-instances";
import { useDesigner } from "../context";
import { listSlots } from "../model/containers";
import { activityDescription, activityDisplayText } from "../model/display";
import { activityAccentColor } from "../model/icons";
import { ActivityIcon } from "./activity-icon";

/**
 * Canvas-independent activity card (used by the sequence designer and slot chips). Shows the
 * icon, title, type and the activity's child slots; clicking a slot opens it.
 */
export function ActivityCard({
  activity,
  descriptor,
  selected,
  readOnly,
  stats,
  badge,
  onSelect,
  warning,
  className,
}: {
  activity: Activity;
  descriptor?: ActivityDescriptor;
  selected?: boolean;
  readOnly?: boolean;
  stats?: ActivityExecutionStats;
  badge?: ReactNode;
  onSelect?: () => void;
  warning?: string;
  className?: string;
}) {
  const { store } = useDesigner();
  const accent = activityAccentColor(descriptor);
  const title = activityDisplayText(activity, descriptor);
  const description = activity.metadata?.showDescription ? activityDescription(activity, descriptor) : undefined;
  const slots = listSlots(activity, descriptor);
  const isContainer = ["Elsa.Flowchart", "Elsa.Sequence", "Elsa.StateMachine"].includes(activity.type);
  const tone = stats?.isFaulted ? "border-status-danger" : stats?.isBlocked ? "border-status-warning" : stats && stats.completedCount > 0 ? "border-status-success/60" : undefined;

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={(e) => {
        e.stopPropagation();
        onSelect?.();
      }}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") onSelect?.();
      }}
      onDoubleClick={(e) => {
        e.stopPropagation();
        if (isContainer) store.getState().enter({ activityId: activity.id, slot: "" });
        else if (!readOnly) store.getState().startRename(activity.id);
      }}
      style={{ "--rail-color": accent } as React.CSSProperties}
      className={cn(
        "accent-rail relative w-full rounded-lg border bg-card text-left text-card-foreground shadow-[0_1px_2px_rgb(15_23_42/0.06),0_8px_24px_-12px_rgb(15_23_42/0.25)] outline-none transition-[box-shadow,border-color] dark:shadow-[0_1px_2px_rgb(0_0_0/0.4)]",
        selected ? "border-primary ring-2 ring-primary/25" : cn("border-border hover:border-muted-foreground/40", tone),
        className,
      )}
    >
      {warning && (
        <span className="absolute -left-2 -top-2 flex size-5 items-center justify-center rounded-full bg-status-warning text-white shadow" title={warning}>
          <TriangleAlert className="size-3" />
        </span>
      )}
      <div className="flex items-start gap-2.5 py-2 pl-3.5 pr-3">
        <span className="mt-0.5 flex size-7 shrink-0 items-center justify-center rounded-md" style={{ background: `color-mix(in oklab, ${accent} 14%, var(--card))`, color: accent }}>
          <ActivityIcon typeName={activity.type} descriptor={descriptor} className="size-4" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <div className="truncate text-sm font-medium leading-tight">{title}</div>
            {badge}
          </div>
          <div className="truncate text-[11px] text-muted-foreground">{descriptor?.displayName ?? activity.type}</div>
          {description && <div className="mt-1 line-clamp-3 text-xs text-muted-foreground">{description}</div>}
        </div>
        {isContainer && (
          <button
            type="button"
            className="shrink-0 rounded-md border px-1.5 py-0.5 text-[11px] text-muted-foreground hover:border-primary hover:text-primary"
            onClick={(e) => {
              e.stopPropagation();
              store.getState().enter({ activityId: activity.id, slot: "" });
            }}
          >
            Open <ChevronRight className="inline size-3" />
          </button>
        )}
      </div>
      {slots.length > 0 && (
        <div className="flex flex-col gap-1 border-t border-dashed px-2 py-1.5">
          {slots.map((s) => (
            <button
              key={s.slot}
              type="button"
              className="flex items-center justify-between gap-2 rounded border border-dashed px-2 py-1 text-left text-[11px] hover:bg-muted"
              onClick={(e) => {
                e.stopPropagation();
                if (s.child) store.getState().enterSlot(activity.id, s.slot);
                else if (!readOnly) store.getState().openQuickAdd({ screen: { x: e.clientX + 8, y: e.clientY - 20 }, slot: { activityId: activity.id, slot: s.slot }, allowContainers: true });
              }}
            >
              <span className="font-medium">{s.label}</span>
              <span className="flex items-center gap-1 text-muted-foreground">
                {s.child ? activityDisplayText(s.child) : "empty"}
                <ChevronRight className="size-3" />
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
