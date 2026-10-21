"use client";

import { memo } from "react";
import { BaseEdge, EdgeLabelRenderer, getBezierPath, type Edge, type EdgeProps } from "@xyflow/react";
import { Database } from "lucide-react";
import { cn } from "@/lib/utils";

export interface DataEdgeData extends Record<string, unknown> {
  label: string;
  kind: "variable" | "input" | "output";
  highlighted: boolean;
  dimmed: boolean;
}

export type DataFlowEdge = Edge<DataEdgeData, "data">;

/** Dotted association from the activity that writes a piece of data to one that reads it (BPMN data-association style). */
function DataEdgeComponent({ id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, data }: EdgeProps<DataFlowEdge>) {
  const [path, labelX, labelY] = getBezierPath({ sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, curvature: 0.4 });
  const colour = data?.kind === "variable" ? "var(--elsa-secondary)" : data?.kind === "input" ? "var(--status-info)" : "var(--status-success)";
  return (
    <>
      <BaseEdge
        id={id}
        path={path}
        style={{ stroke: colour, strokeWidth: data?.highlighted ? 2 : 1.25, strokeDasharray: "3 4", opacity: data?.dimmed ? 0.25 : 0.85 }}
        interactionWidth={0}
      />
      <EdgeLabelRenderer>
        <div
          className={cn("pointer-events-none absolute flex items-center gap-1 rounded-full border bg-card px-1.5 py-0.5 text-[10px] font-medium shadow-sm", data?.dimmed && "opacity-30")}
          style={{ transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`, color: colour, borderColor: `color-mix(in oklab, ${colour} 40%, transparent)` }}
        >
          <Database className="size-2.5" />
          {data?.label}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}

export const DataEdge = memo(DataEdgeComponent);
