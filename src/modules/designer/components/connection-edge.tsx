"use client";

import { memo } from "react";
import { BaseEdge, EdgeLabelRenderer, getSmoothStepPath, type Edge, type EdgeProps } from "@xyflow/react";
import { Plus, X } from "lucide-react";
import { cn } from "@/lib/utils";
import type { Connection } from "@/lib/api/types/activities";
import { useDesigner } from "../context";

export interface ConnectionEdgeData extends Record<string, unknown> {
  connection: Connection;
  label?: string;
  readOnly: boolean;
  /** Highlighted when the source activity ran in the viewed instance. */
  traversed?: boolean;
}

export type ConnectionFlowEdge = Edge<ConnectionEdgeData, "connection">;

function ConnectionEdgeComponent({ id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, data, selected, markerEnd }: EdgeProps<ConnectionFlowEdge>) {
  const { store } = useDesigner();
  const vertices = data?.connection?.vertices ?? [];
  const [path, labelX, labelY] =
    vertices.length > 0
      ? orthogonalPath([{ x: sourceX, y: sourceY }, ...vertices, { x: targetX, y: targetY }])
      : getSmoothStepPath({ sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, borderRadius: 8 });

  return (
    <>
      <BaseEdge id={id} path={path} markerEnd={markerEnd} style={{ strokeWidth: selected ? 2.5 : 1.75 }} />
      <EdgeLabelRenderer>
        <div
          className="nodrag nopan pointer-events-auto absolute flex items-center gap-1"
          style={{ transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)` }}
        >
          {data?.label && (
            <span className={cn("rounded-full border bg-card px-2 py-0.5 text-[10px] font-semibold shadow-sm", selected ? "border-primary text-primary" : "text-muted-foreground")}>{data.label}</span>
          )}
          {!data?.readOnly && data?.connection && (
            <button
              type="button"
              className={cn(
                "flex size-5 items-center justify-center rounded-full border bg-card text-muted-foreground shadow-sm transition-opacity hover:border-primary hover:text-primary",
                selected ? "opacity-100" : "opacity-0 [.react-flow__edge:hover_&]:opacity-100",
              )}
              onClick={(e) => {
                e.stopPropagation();
                store.getState().openQuickAdd({ screen: { x: e.clientX + 12, y: e.clientY - 20 }, insertInto: data.connection });
              }}
              title="Insert activity here"
            >
              <Plus className="size-3" />
            </button>
          )}
          {selected && !data?.readOnly && data?.connection && (
            <button
              type="button"
              className="flex size-5 items-center justify-center rounded-full border bg-card text-muted-foreground shadow-sm hover:bg-status-danger hover:text-white"
              onClick={(e) => {
                e.stopPropagation();
                store.getState().removeConnections([data.connection]);
              }}
              title="Remove connection"
            >
              <X className="size-3" />
            </button>
          )}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}

/**
 * Horizontal-first orthogonal route through waypoints (dagre places them in the empty column slots
 * between ranks), with rounded corners. Returns the path and a label position on the longest segment.
 */
function orthogonalPath(points: { x: number; y: number }[], radius = 8): [string, number, number] {
  // Expand to axis-aligned corners: between consecutive points go horizontal, then vertical.
  const pts: { x: number; y: number }[] = [points[0]];
  for (let i = 1; i < points.length; i++) {
    const prev = pts[pts.length - 1];
    const next = points[i];
    const last = i === points.length - 1;
    if (last) {
      // Approach the target horizontally: bend halfway between the last waypoint and the target.
      const midX = prev.x + Math.max(20, (next.x - prev.x) / 2);
      if (Math.abs(prev.y - next.y) > 0.5) pts.push({ x: midX, y: prev.y }, { x: midX, y: next.y });
    } else if (Math.abs(prev.y - next.y) > 0.5 && Math.abs(prev.x - next.x) > 0.5) {
      pts.push({ x: next.x, y: prev.y });
    }
    pts.push(next);
  }
  // Drop collinear duplicates.
  const clean = pts.filter((p, i) => i === 0 || Math.abs(p.x - pts[i - 1].x) > 0.5 || Math.abs(p.y - pts[i - 1].y) > 0.5);
  let d = `M ${clean[0].x} ${clean[0].y}`;
  for (let i = 1; i < clean.length; i++) {
    const p = clean[i];
    const prev = clean[i - 1];
    const next = clean[i + 1];
    if (!next) {
      d += ` L ${p.x} ${p.y}`;
      break;
    }
    const r = Math.min(radius, Math.hypot(p.x - prev.x, p.y - prev.y) / 2, Math.hypot(next.x - p.x, next.y - p.y) / 2);
    const inX = p.x - Math.sign(p.x - prev.x) * r;
    const inY = p.y - Math.sign(p.y - prev.y) * r;
    const outX = p.x + Math.sign(next.x - p.x) * r;
    const outY = p.y + Math.sign(next.y - p.y) * r;
    d += ` L ${inX} ${inY} Q ${p.x} ${p.y} ${outX} ${outY}`;
  }
  // Label on the longest horizontal segment.
  let best = { len: -1, x: clean[0].x, y: clean[0].y };
  for (let i = 1; i < clean.length; i++) {
    const a = clean[i - 1];
    const b = clean[i];
    const len = Math.abs(b.x - a.x);
    if (Math.abs(a.y - b.y) < 0.5 && len > best.len) best = { len, x: (a.x + b.x) / 2, y: a.y };
  }
  return [d, best.x, best.y];
}

export const ConnectionEdge = memo(ConnectionEdgeComponent);
