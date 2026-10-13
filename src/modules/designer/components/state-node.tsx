"use client";

import { memo, useEffect, useRef, useState } from "react";
import { Handle, Position, type Node, type NodeProps } from "@xyflow/react";
import { ChevronRight, CirclePlay, LogIn, LogOut, Plus, TriangleAlert } from "lucide-react";
import { cn } from "@/lib/utils";
import type { Activity, StateMachineState } from "@/lib/api/types/activities";
import type { ActivityExecutionStats } from "@/lib/api/types/workflow-instances";
import { useDesigner, useDesignerState } from "../context";
import { activityDisplayText } from "../model/display";
import { selectionId } from "../store";

export interface StateNodeData extends Record<string, unknown> {
  state: StateMachineState;
  index: number;
  isInitial: boolean;
  isCurrent: boolean;
  readOnly: boolean;
  warning?: string;
  entryStats?: ActivityExecutionStats;
  smId: string;
}

export type StateFlowNode = Node<StateNodeData, "state">;

function StateNodeComponent({ data, selected }: NodeProps<StateFlowNode>) {
  const { state, index, isInitial, isCurrent, readOnly, warning, smId } = data;
  const { store } = useDesigner();
  const renaming = useDesignerState((s) => s.renamingId === selectionId.state(state.name));
  const connectPending = useDesignerState((s) => !!s.connectFrom && s.connectFrom.activityId !== `state:${state.name}`);

  const slot = (kind: "entry" | "exit", child: Activity | null | undefined, e: React.MouseEvent) => {
    e.stopPropagation();
    const path = `states.${index}.${kind}`;
    if (child) store.getState().enterSlot(smId, path);
    else if (!readOnly) store.getState().openQuickAdd({ screen: { x: e.clientX + 8, y: e.clientY - 20 }, slot: { activityId: smId, slot: path }, allowContainers: true });
  };

  return (
    <div
      className={cn(
        "group/state relative min-w-[200px] max-w-[260px] rounded-xl border-2 bg-card text-card-foreground shadow-[0_1px_2px_rgb(15_23_42/0.06),0_8px_24px_-12px_rgb(15_23_42/0.25)] transition-[box-shadow,border-color] dark:shadow-[0_1px_2px_rgb(0_0_0/0.4)]",
        selected ? "border-primary ring-2 ring-primary/25" : "border-border hover:border-muted-foreground/40",
        isCurrent && !selected && "border-status-info",
        connectPending && "cursor-crosshair ring-2 ring-status-success/40 border-status-success",
      )}
      onClickCapture={(e) => {
        if (!connectPending) return;
        e.stopPropagation();
        const from = store.getState().connectFrom!;
        store.getState().startConnect(undefined);
        store.getState().addTransition({ from: from.activityId.replace(/^state:/, ""), to: state.name });
      }}
      onDoubleClick={(e) => {
        if (readOnly) return;
        e.stopPropagation();
        store.getState().startRename(selectionId.state(state.name));
      }}
    >
      {/* Transitions enter on the left and leave on the right; the ids are fixed so edges stay stable. */}
      <Handle type="target" position={Position.Left} id="in" className="!h-7 !w-4 !rounded-sm !border-0 !bg-transparent" style={{ top: "50%", left: -1, transform: "translate(0, -50%)" }} isConnectable={!readOnly} />
      <Handle type="source" position={Position.Right} id="out" className="!size-3 !rounded-full !border-2 !border-card !bg-primary" style={{ top: "50%" }} isConnectable={!readOnly} />

      {isInitial && (
        <span className="absolute -left-2.5 -top-2.5 flex size-6 items-center justify-center rounded-full bg-primary text-primary-foreground shadow" title="Initial state">
          <CirclePlay className="size-3.5" />
        </span>
      )}
      {warning && (
        <span className="absolute -right-2 -top-2 flex size-5 items-center justify-center rounded-full bg-status-warning text-white shadow" title={warning}>
          <TriangleAlert className="size-3" />
        </span>
      )}

      <div className="px-3 pt-2.5 pb-2">
        <div className="eyebrow text-[9px]">{isCurrent ? "Current state" : "State"}</div>
        {renaming ? (
          <RenameState name={state.name} onDone={() => store.getState().startRename(undefined)} />
        ) : (
          <div className="truncate text-sm font-semibold leading-tight">{state.name || <span className="italic text-muted-foreground">(unnamed)</span>}</div>
        )}
      </div>

      <div className="flex flex-col gap-1 border-t border-dashed px-2 py-1.5">
        {(["entry", "exit"] as const).map((kind) => {
          const child = state[kind];
          return (
            <button
              key={kind}
              type="button"
              className="nodrag flex items-center justify-between gap-2 rounded border border-dashed px-2 py-1 text-left text-[11px] hover:bg-muted"
              onClick={(e) => slot(kind, child, e)}
              title={child ? `Open ${kind} activity` : `Add ${kind} activity`}
            >
              <span className="flex items-center gap-1 font-medium text-muted-foreground">
                {kind === "entry" ? <LogIn className="size-3" /> : <LogOut className="size-3" />}
                {kind === "entry" ? "On entry" : "On exit"}
              </span>
              <span className="flex items-center gap-1 truncate">
                {child ? <span className="truncate text-foreground">{activityDisplayText(child)}</span> : <span className="text-muted-foreground">none</span>}
                {child ? <ChevronRight className="size-3" /> : !readOnly && <Plus className="size-3" />}
              </span>
            </button>
          );
        })}
      </div>

      {!readOnly && (
        <button
          type="button"
          aria-label="Add transition"
          title="Add a transition from this state"
          className="nodrag absolute -right-8 top-1/2 flex size-5 -translate-y-1/2 items-center justify-center rounded-full border bg-card text-muted-foreground opacity-0 shadow-sm transition-opacity hover:border-primary hover:text-primary group-hover/state:opacity-100 data-[selected=true]:opacity-100"
          data-selected={selected}
          onClick={(e) => {
            e.stopPropagation();
            store.getState().startConnect({ activityId: `state:${state.name}`, port: "out" });
          }}
          onMouseDown={(e) => e.stopPropagation()}
        >
          <Plus className="size-3" />
        </button>
      )}
    </div>
  );
}

function RenameState({ name, onDone }: { name: string; onDone: () => void }) {
  const { store } = useDesigner();
  const [value, setValue] = useState(name);
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    ref.current?.focus();
    ref.current?.select();
  }, []);
  const commit = () => {
    if (value.trim() && value.trim() !== name) store.getState().renameState(name, value.trim());
    onDone();
  };
  return (
    <input
      ref={ref}
      className="nodrag w-full rounded border border-primary bg-background px-1 text-sm font-semibold outline-none"
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

export const StateNode = memo(StateNodeComponent);
