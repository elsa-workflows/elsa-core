"use client";

import { memo } from "react";
import { BaseEdge, EdgeLabelRenderer, getSmoothStepPath, type Edge, type EdgeProps } from "@xyflow/react";
import { Zap, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { useDesigner } from "../context";

export interface TransitionEdgeData extends Record<string, unknown> {
  index: number;
  label: string;
  /** Display name of the trigger activity, if any; automatic transitions have none. */
  trigger?: string;
  hasCondition: boolean;
  readOnly: boolean;
  isSelf: boolean;
  warning?: string;
  /** Selected through the store (side pane / state panel) rather than by clicking the edge. */
  highlighted?: boolean;
}

export type TransitionFlowEdge = Edge<TransitionEdgeData, "transition">;

/** A state-machine transition. Self-transitions loop over the top of the state. */
function TransitionEdgeComponent({ id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, data, selected: rfSelected, markerEnd }: EdgeProps<TransitionFlowEdge>) {
  const { store } = useDesigner();
  const selected = rfSelected || data?.highlighted;
  let path: string;
  let labelX: number;
  let labelY: number;
  if (data?.isSelf) {
    const r = 36;
    path = `M ${sourceX} ${sourceY} C ${sourceX + 60} ${sourceY - r * 2}, ${targetX - 60} ${targetY - r * 2}, ${targetX} ${targetY}`;
    labelX = (sourceX + targetX) / 2;
    labelY = sourceY - r * 1.5 - 8;
  } else {
    [path, labelX, labelY] = getSmoothStepPath({ sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, borderRadius: 10 });
  }
  return (
    <>
      <BaseEdge id={id} path={path} markerEnd={markerEnd} style={{ strokeWidth: selected ? 2.5 : 2, strokeDasharray: data?.trigger ? undefined : "6 4" }} />
      <EdgeLabelRenderer>
        <div
          className="nodrag nopan pointer-events-auto absolute flex items-center gap-1"
          style={{ transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)` }}
          onClick={(e) => {
            e.stopPropagation();
            store.getState().select([`transition:${data?.index}`]);
          }}
        >
          <span
            className={cn(
              "flex max-w-48 items-center gap-1 rounded-full border bg-card px-2 py-0.5 text-[11px] font-medium shadow-sm",
              selected ? "border-primary text-primary" : "text-foreground",
              data?.warning && "border-status-warning",
            )}
            title={data?.warning ?? (data?.trigger ? `Trigger: ${data.trigger}` : "Automatic transition")}
          >
            {data?.trigger ? <Zap className="size-3 text-status-warning" /> : null}
            <span className="truncate">{data?.label}</span>
            {data?.hasCondition && <span className="text-muted-foreground">?</span>}
          </span>
          {selected && !data?.readOnly && (
            <button
              type="button"
              className="flex size-5 items-center justify-center rounded-full border bg-card text-muted-foreground shadow-sm hover:bg-status-danger hover:text-white"
              onClick={(e) => {
                e.stopPropagation();
                store.getState().removeTransitions([data!.index]);
              }}
              title="Remove transition"
            >
              <X className="size-3" />
            </button>
          )}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}

export const TransitionEdge = memo(TransitionEdgeComponent);
