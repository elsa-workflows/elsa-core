"use client";

import { memo } from "react";
import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { ContextBoxNode as ContextBoxNodeType } from "./decorations";

/**
 * The grouping box drawn around the current workflow in call-context mode. It sits behind the
 * activities and ignores the pointer, so everything inside behaves exactly as without it. Four hidden
 * handles let decoration edges attach on either side (callers enter on the left, callees leave on the
 * right, cycle edges use the opposite sides).
 */
function ContextBoxNodeComponent({ data, width, height }: NodeProps<ContextBoxNodeType>) {
  return (
    <div className="pointer-events-none rounded-2xl border-2 border-dashed border-primary/40 bg-primary/[0.03]" style={{ width, height }}>
      <div className="absolute -top-3.5 left-4 flex items-center gap-2 rounded-full border border-primary/40 bg-card px-2.5 py-0.5 text-[11px] font-semibold text-primary shadow-sm">
        {data.title}
        {data.subtitle && <span className="font-normal text-muted-foreground">{data.subtitle}</span>}
      </div>
      <Handle type="target" position={Position.Left} id="in" isConnectable={false} className="!pointer-events-none !opacity-0" />
      <Handle type="source" position={Position.Right} id="out" isConnectable={false} className="!pointer-events-none !opacity-0" />
      <Handle type="target" position={Position.Right} id="in-right" isConnectable={false} className="!pointer-events-none !opacity-0" />
      <Handle type="source" position={Position.Left} id="out-left" isConnectable={false} className="!pointer-events-none !opacity-0" />
    </div>
  );
}

export const ContextBoxNode = memo(ContextBoxNodeComponent);
