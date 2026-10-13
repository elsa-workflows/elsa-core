"use client";

import { memo } from "react";
import { Handle, Position, type NodeProps } from "@xyflow/react";
import { ArrowUpRight, ChevronsRight, CircleHelp, FileWarning, Loader2, Workflow } from "lucide-react";
import { cn } from "@/lib/utils";
import type { ContextWorkflowNode as ContextWorkflowNodeType } from "./decorations";

const kinds = {
  workflow: { icon: Workflow, accent: "var(--primary)", title: "Workflow" },
  missing: { icon: FileWarning, accent: "var(--status-danger)", title: "Referenced workflow does not exist or could not be loaded" },
  unresolved: { icon: CircleHelp, accent: "var(--status-warning)", title: "The target is computed by an expression and cannot be resolved here" },
  truncated: { icon: ChevronsRight, accent: "var(--muted-foreground)", title: "More workflows beyond this point" },
  pending: { icon: Loader2, accent: "var(--muted-foreground)", title: "Loading…" },
} as const;

/** An external workflow (caller or callee) in call-context mode, with a drill-in action. */
function ContextWorkflowNodeComponent({ data }: NodeProps<ContextWorkflowNodeType>) {
  const meta = kinds[data.kind];
  const Icon = meta.icon;
  const placeholder = data.kind !== "workflow";
  return (
    <div
      className={cn("node-card accent-rail relative flex h-[60px] w-[200px] items-center gap-2.5 rounded-xl border bg-card pl-3.5 pr-2 shadow-sm", placeholder && "border-dashed")}
      style={{ "--rail-color": meta.accent } as React.CSSProperties}
      title={meta.title}
    >
      <span className="flex size-8 shrink-0 items-center justify-center rounded-lg" style={{ background: `color-mix(in oklab, ${meta.accent} 14%, var(--card))`, color: meta.accent }}>
        <Icon className={cn("size-4", data.kind === "pending" && "animate-spin")} />
      </span>
      <div className="min-w-0 flex-1">
        <div className={cn("truncate text-sm font-medium", placeholder && "text-muted-foreground")}>{data.name}</div>
        {data.subtitle && <div className="truncate text-[11px] text-muted-foreground">{data.subtitle}</div>}
      </div>
      {data.onOpen && (
        <button
          type="button"
          className="nodrag nopan flex size-7 shrink-0 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-primary"
          onClick={(e) => {
            e.stopPropagation();
            data.onOpen?.();
          }}
          title="Open this workflow"
          aria-label={`Open ${data.name}`}
        >
          <ArrowUpRight className="size-4" />
        </button>
      )}
      {data.onExpand && (
        <button
          type="button"
          className="nodrag nopan rounded-md border px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground hover:bg-accent hover:text-foreground"
          onClick={(e) => {
            e.stopPropagation();
            data.onExpand?.();
          }}
        >
          Expand
        </button>
      )}
      <Handle type="target" position={Position.Left} id="in-left" isConnectable={false} className="!pointer-events-none !opacity-0" />
      <Handle type="target" position={Position.Right} id="in-right" isConnectable={false} className="!pointer-events-none !opacity-0" />
      <Handle type="source" position={Position.Right} id="out-right" isConnectable={false} className="!pointer-events-none !opacity-0" />
      <Handle type="source" position={Position.Left} id="out-left" isConnectable={false} className="!pointer-events-none !opacity-0" />
    </div>
  );
}

export const ContextWorkflowNode = memo(ContextWorkflowNodeComponent);
