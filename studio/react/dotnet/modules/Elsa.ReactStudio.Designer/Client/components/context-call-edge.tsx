"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { memo } from "react";
import { BaseEdge, EdgeLabelRenderer, getSmoothStepPath, type EdgeProps } from "@xyflow/react";
import { cn } from "@elsa-workflows/react-studio-ui/utils";
import type { DecorationEdge } from "@elsa-workflows/react-studio-sdk/designer/decorations";

/** A call between workflows in call-context mode: labelled with the call type, dashed when it closes a cycle. */
function ContextCallEdgeComponent({ id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, data, markerEnd }: EdgeProps<DecorationEdge>) {
  useTranslation();
  const [path, labelX, labelY] = getSmoothStepPath({ sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, borderRadius: 10 });
  const back = !!data?.back;
  return (
    <>
      <BaseEdge
        id={id}
        path={path}
        markerEnd={markerEnd}
        interactionWidth={0}
        style={{ stroke: back ? "var(--status-warning)" : "var(--primary)", strokeWidth: 1.75, strokeDasharray: back ? "6 4" : undefined, opacity: 0.85 }}
      />
      <EdgeLabelRenderer>
        <div
          className={cn(
            "pointer-events-none absolute rounded-full border bg-card px-2 py-0.5 text-[10px] font-semibold shadow-sm",
            back ? "border-status-warning/50 text-status-warning" : "border-primary/40 text-primary",
          )}
          style={{ transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)` }}
        >
          {data?.label}
          {back && " · cycle"}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}

export const ContextCallEdge = memo(ContextCallEdgeComponent);
