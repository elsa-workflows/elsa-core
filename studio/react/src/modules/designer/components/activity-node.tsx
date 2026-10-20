"use client";

import { memo, useEffect, useRef, useState } from "react";
import { Handle, Position, type Node, type NodeProps } from "@xyflow/react";
import { AlertTriangle, ChevronRight, Hourglass, PlayCircle, Plus, TriangleAlert, Zap } from "lucide-react";
import { cn } from "@/lib/utils";
import type { Activity } from "@/lib/api/types/activities";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";
import type { ActivityExecutionStats } from "@/lib/api/types/workflow-instances";
import { isFlowchart } from "@/lib/api/types/activities";
import { activityDescription, activityDisplayText } from "../model/display";
import { activityAccentColor } from "../model/icons";
import { ActivityIcon } from "./activity-icon";
import { portPropertyName } from "../model/ids";
import type { ActivityPorts } from "../model/ports";
import { useDesigner, useDesignerState } from "../context";
import { NodeContextToolbar } from "./node-context-toolbar";
import type { DesignerIssue } from "../model/validation";
import type { ActivityDataSummary } from "../model/data-flow";

export interface ActivityNodeData extends Record<string, unknown> {
  activity: Activity;
  descriptor?: ActivityDescriptor;
  ports: ActivityPorts;
  stats?: ActivityExecutionStats;
  readOnly: boolean;
  issues?: DesignerIssue[];
  /** Data read/written by this activity, shown as tags when the data-flow overlay is on. */
  dataSummary?: ActivityDataSummary;
  /** Role of this activity for the highlighted piece of data. */
  dataRole?: "writer" | "reader" | "both";
}

export type ActivityFlowNode = Node<ActivityNodeData, "activity">;

function StatsBadge({ stats }: { stats: ActivityExecutionStats }) {
  const tone = stats.isFaulted
    ? "bg-red-500 text-white"
    : stats.isBlocked
      ? "bg-amber-500 text-white"
      : stats.uncompletedCount > stats.completedCount
        ? "bg-sky-500 text-white"
        : stats.completedCount > 0
          ? "bg-emerald-500 text-white"
          : "bg-muted text-muted-foreground";
  const count = stats.isFaulted || stats.isBlocked ? stats.startedCount : stats.completedCount;
  return (
    <span
      className={cn("absolute -right-2 -top-2 flex h-5 min-w-5 items-center justify-center gap-0.5 rounded-full px-1.5 text-[10px] font-semibold shadow", tone)}
      title={`started ${stats.startedCount}, completed ${stats.completedCount}, uncompleted ${stats.uncompletedCount}`}
    >
      {stats.isFaulted ? <AlertTriangle className="size-3" /> : stats.isBlocked ? <Hourglass className="size-3" /> : stats.uncompletedCount > 0 ? <PlayCircle className="size-3" /> : null}
      {count}
    </span>
  );
}

function InlineRename({ activity, onDone }: { activity: Activity; onDone: () => void }) {
  const { store } = useDesigner();
  const [value, setValue] = useState(activity.name ?? "");
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    ref.current?.focus();
    ref.current?.select();
  }, []);
  const commit = () => {
    const name = value.trim();
    if (name && name !== activity.name) store.getState().updateActivity(activity.id, (a) => ({ ...a, name }));
    onDone();
  };
  return (
    <input
      ref={ref}
      className="nodrag w-full rounded border border-primary bg-background px-1 text-sm font-medium outline-none"
      value={value}
      onChange={(e) => setValue(e.target.value)}
      onBlur={commit}
      onKeyDown={(e) => {
        e.stopPropagation();
        if (e.key === "Enter") commit();
        if (e.key === "Escape") onDone();
      }}
      onMouseDown={(e) => e.stopPropagation()}
    />
  );
}

function ActivityNodeComponent({ data, selected }: NodeProps<ActivityFlowNode>) {
  const { activity, descriptor, ports, stats, readOnly, issues, dataSummary, dataRole } = data;
  const highlightActive = useDesignerState((s) => !!s.dataHighlight);
  const { store } = useDesigner();
  const renaming = useDesignerState((s) => s.renamingId === activity.id);
  const connectPending = useDesignerState((s) => !!s.connectFrom && s.connectFrom.activityId !== activity.id && !!ports.inPort);
  const warning = issues?.find((i) => i.severity === "warning");
  const accent = activityAccentColor(descriptor);
  const title = activityDisplayText(activity, descriptor);
  const description = activity.metadata?.showDescription ? activityDescription(activity, descriptor) : undefined;
  const canStart = activity.customProperties?.canStartWorkflow === true;
  const faulted = stats?.isFaulted;

  return (
    <div
      style={{ "--rail-color": accent } as React.CSSProperties}
      className={cn(
        "accent-rail relative min-w-[210px] max-w-[280px] rounded-lg border bg-card text-card-foreground transition-[box-shadow,border-color]",
        "shadow-[0_1px_2px_rgb(15_23_42/0.06),0_8px_24px_-12px_rgb(15_23_42/0.25)] dark:shadow-[0_1px_2px_rgb(0_0_0/0.4)]",
        selected ? "border-primary ring-2 ring-primary/25" : "border-border hover:border-muted-foreground/40",
        faulted && !selected && "border-status-danger",
        stats?.isBlocked && !selected && "border-status-warning",
        "group/node",
        connectPending && "cursor-crosshair ring-2 ring-status-success/40 border-status-success",
        highlightActive && !dataRole && "opacity-40",
        dataRole === "writer" && "ring-2 ring-status-warning/50 border-status-warning",
        dataRole === "reader" && "ring-2 ring-status-info/50 border-status-info",
        dataRole === "both" && "ring-2 ring-primary/50 border-primary",
      )}
      onClickCapture={(e) => {
        if (!connectPending) return;
        e.stopPropagation();
        store.getState().completeConnect(activity.id);
      }}
      onDoubleClick={(e) => {
        if (readOnly) return;
        e.stopPropagation();
        store.getState().startRename(activity.id);
      }}
    >
      {!readOnly && <NodeContextToolbar activity={activity} descriptor={descriptor} ports={ports} visible={!!selected && !renaming} />}
      {warning && !stats && (
        <span className="absolute -left-2 -top-2 flex size-5 items-center justify-center rounded-full bg-status-warning text-white shadow" title={warning.message}>
          <TriangleAlert className="size-3" />
        </span>
      )}
      {stats && <StatsBadge stats={stats} />}
      {/* Invisible handles so data-flow associations route top→bottom, away from the control-flow ports. */}
      <Handle type="target" position={Position.Top} id="__data-in" isConnectable={false} className="!pointer-events-none !opacity-0" />
      <Handle type="source" position={Position.Bottom} id="__data-out" isConnectable={false} className="!pointer-events-none !opacity-0" />
      {/* Anchor for call-context edges that leave this specific step (Dispatch / Execute Workflow). */}
      <Handle type="source" position={Position.Right} id="__context-out" isConnectable={false} className="!pointer-events-none !opacity-0" />

      {/* In-port and out-ports anchor to the full card height, so both sit at the activity's vertical middle. */}
      {ports.inPort && (
        <Handle
          type="target"
          position={Position.Left}
          id={ports.inPort}
          className={cn(
            "!h-7 !w-4 !rounded-sm !border-0 !bg-transparent transition-colors",
            "[.react-flow.connecting_&]:!bg-primary/20 [.react-flow.connecting_&]:!shadow-[inset_0_0_0_1px_var(--primary)]",
          )}
          // React Flow ends an incoming edge at the handle's outer boundary, so the zone starts exactly on the border.
          style={{ top: "50%", left: -1, transform: "translate(0, -50%)" }}
          isConnectable={!readOnly}
        />
      )}
      <div className="flex items-stretch">
        <div className="flex min-w-0 flex-1 flex-col">
          <div className="flex min-w-0 flex-1 items-start gap-2.5 py-2 pl-3.5 pr-2">
            <span className="mt-0.5 flex size-7 shrink-0 items-center justify-center rounded-md" style={{ background: `color-mix(in oklab, ${accent} 14%, var(--card))`, color: accent }}>
              <ActivityIcon typeName={activity.type} descriptor={descriptor} className="size-4" />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-1">
                {renaming ? <InlineRename activity={activity} onDone={() => store.getState().startRename(undefined)} /> : <div className="truncate text-sm font-medium leading-tight">{title}</div>}
                {canStart && (
                  <span title="Can start workflow">
                    <Zap className="size-3 text-amber-500" />
                  </span>
                )}
              </div>
              <div className="truncate text-[11px] text-muted-foreground">{descriptor?.displayName ?? activity.type}</div>
              {description && <div className="mt-1 line-clamp-3 text-xs text-muted-foreground">{description}</div>}
            </div>
          </div>

          {dataSummary && (dataSummary.writes.length > 0 || dataSummary.reads.length > 0) && (
            <div className="flex flex-wrap gap-1 border-t border-dashed px-2.5 py-1.5">
              {dataSummary.writes.map((d) => (
                <span key={`w-${d.kind}-${d.key}`} className="rounded-full bg-status-warning/15 px-1.5 text-[10px] font-medium text-status-warning" title={`Writes ${d.kind} ${d.name}`}>
                  ↑ {d.name}
                </span>
              ))}
              {dataSummary.reads.map((d) => (
                <span key={`r-${d.kind}-${d.key}`} className="rounded-full bg-status-info/15 px-1.5 text-[10px] font-medium text-status-info" title={`Reads ${d.kind} ${d.name}`}>
                  ↓ {d.name}
                </span>
              ))}
            </div>
          )}
          {ports.embeddedPorts.length > 0 && (
            <div className="border-t border-dashed px-2 py-1.5 flex flex-col gap-1">
              {ports.embeddedPorts.map((port) => {
                const child = activity[portPropertyName(port.name)] as Activity | undefined;
                const label = child ? (isFlowchart(child) ? `${child.activities.length} activities` : child.type) : "empty";
                return (
                  <button
                    key={port.name}
                    type="button"
                    className="nodrag flex items-center justify-between gap-2 rounded border border-dashed px-2 py-1 text-left text-[11px] hover:bg-muted"
                    onClick={(e) => {
                      e.stopPropagation();
                      const slot = portPropertyName(port.name);
                      if (child) store.getState().enterSlot(activity.id, slot);
                      else if (!readOnly) store.getState().openQuickAdd({ screen: { x: e.clientX + 8, y: e.clientY - 20 }, slot: { activityId: activity.id, slot }, allowContainers: true });
                    }}
                    title={child ? `Open ${port.displayName}` : `Add activities to ${port.displayName}`}
                  >
                    <span className="font-medium">{port.displayName}</span>
                    <span className="flex items-center gap-1 text-muted-foreground">
                      {label}
                      <ChevronRight className="size-3" />
                    </span>
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {ports.outPorts.length > 0 && (
          <div className={cn("flex shrink-0 flex-col justify-around self-stretch", ports.outPorts.length > 1 && "border-l border-dashed py-1")}>
            {ports.outPorts.map((port) => (
              <div key={port.name} className="relative flex h-6 items-center justify-end text-[11px] font-medium text-muted-foreground">
                {ports.outPorts.length > 1 ? <span className="px-3">{port.displayName}</span> : <span className="w-3" />}
                {!readOnly && (
                  <button
                    type="button"
                    aria-label={`Add activity after ${port.displayName}`}
                    title={`Add activity after ${port.displayName}`}
                    className="nodrag absolute -right-8 top-1/2 flex size-5 -translate-y-1/2 items-center justify-center rounded-full border bg-card text-muted-foreground opacity-0 shadow-sm transition-opacity hover:border-primary hover:text-primary group-hover/node:opacity-100 data-[selected=true]:opacity-100"
                    data-selected={selected}
                    onClick={(e) => {
                      e.stopPropagation();
                      store.getState().openQuickAdd({ screen: { x: e.clientX + 12, y: e.clientY - 20 }, source: { activityId: activity.id, port: port.name } });
                    }}
                    onMouseDown={(e) => e.stopPropagation()}
                  >
                    <Plus className="size-3" />
                  </button>
                )}
                <Handle
                  type="source"
                  position={Position.Right}
                  id={port.name}
                  title={port.displayName}
                  className="!size-3 !rounded-full !border-2 !border-card !bg-primary"
                  style={{ top: "50%" }}
                  isConnectable={!readOnly}
                />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export const ActivityNode = memo(ActivityNodeComponent);
