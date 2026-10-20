"use client";

import { useCallback, useEffect, useMemo, useRef, type KeyboardEvent } from "react";
import {
  Background,
  BackgroundVariant,
  MarkerType,
  MiniMap,
  Panel,
  ReactFlow,
  useNodesInitialized,
  useReactFlow,
  useViewport,
  type Connection as FlowConnection,
  type NodeChange,
} from "@xyflow/react";
import { LayoutGrid, Map as MapIcon, Maximize, Minus, MousePointerClick, PanelLeftOpen, Plus, Redo2, Undo2, X } from "lucide-react";
import { layoutGraph } from "../model/layout";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { isStateMachine, type StateMachine } from "@/lib/api/types/activities";
import { useDesigner, useDesignerState } from "../context";
import { resolveContainer } from "../model/flowchart-ops";
import { validateWorkflow } from "../model/validation";
import { DesignerBreadcrumb } from "./designer-breadcrumb";
import { IconButton } from "./designer-canvas";
import { QuickAddMenu } from "./quick-add-menu";
import { StateNode, type StateFlowNode } from "./state-node";
import { TransitionEdge, type TransitionFlowEdge } from "./transition-edge";
import { selectionId, stateMachineDesignerMetadata } from "../store";

const nodeTypes = { state: StateNode };
const edgeTypes = { transition: TransitionEdge };

/**
 * Designer for `Elsa.StateMachine`: states are nodes, transitions are edges. Drag from a state's
 * port to another state (or itself) to add a transition; the side pane edits the selected state or
 * transition (trigger, condition, action). Positions live in the activity's designer metadata.
 */
export function StateMachineCanvas({ minimap = true, onToggleMinimap, onOpenToolbox, className }: { minimap?: boolean; onToggleMinimap?: () => void; onOpenToolbox?: () => void; className?: string }) {
  const { store, registry, stats } = useDesigner();
  const root = useDesignerState((s) => s.root);
  const path = useDesignerState((s) => s.path);
  const selectedIds = useDesignerState((s) => s.selectedIds);
  const readOnly = useDesignerState((s) => s.readOnly);
  const canUndo = useDesignerState((s) => s.past.length > 0);
  const canRedo = useDesignerState((s) => s.future.length > 0);
  const connectFrom = useDesignerState((s) => s.connectFrom);
  const flow = useReactFlow<StateFlowNode, TransitionFlowEdge>();
  const wrapperRef = useRef<HTMLDivElement>(null);
  const dragSnapshot = useRef<typeof root>(null);

  const sm = useMemo<StateMachine | undefined>(() => {
    const c = resolveContainer(root, path);
    return isStateMachine(c) ? c : undefined;
  }, [root, path]);

  const issues = useMemo(() => (readOnly ? [] : validateWorkflow(root, registry).filter((i) => i.stateName || i.transitionIndex !== undefined)), [root, registry, readOnly]);

  const nodes = useMemo<StateFlowNode[]>(() => {
    if (!sm) return [];
    const positions = stateMachineDesignerMetadata(sm).states ?? {};
    return sm.states.map((state, index) => ({
      id: selectionId.state(state.name),
      type: "state",
      position: positions[state.name]?.position ?? { x: 80 + index * 280, y: 120 },
      selected: selectedIds.includes(selectionId.state(state.name)),
      draggable: !readOnly,
      data: {
        state,
        index,
        isInitial: sm.initialState === state.name,
        isCurrent: sm.currentState === state.name,
        readOnly,
        warning: issues.find((i) => i.stateName === state.name && i.severity === "warning")?.message,
        entryStats: state.entry ? stats?.get(state.entry.nodeId ?? state.entry.id) : undefined,
        smId: sm.id,
      },
    }));
  }, [sm, selectedIds, readOnly, issues, stats]);

  const edges = useMemo<TransitionFlowEdge[]>(() => {
    if (!sm) return [];
    return sm.transitions.map((t, index) => {
      const triggerDescriptor = t.trigger ? registry.get(t.trigger.type, t.trigger.version) : undefined;
      const id = selectionId.transition(index);
      return {
        id,
        type: "transition",
        source: selectionId.state(t.from),
        sourceHandle: "out",
        target: selectionId.state(t.to),
        targetHandle: "in",
        markerEnd: { type: MarkerType.ArrowClosed, width: 14, height: 14, color: "var(--xy-edge-stroke)" },
        data: {
          index,
          highlighted: selectedIds.includes(id),
          label: t.displayName || t.name || (t.trigger ? (t.trigger.name ?? triggerDescriptor?.displayName ?? t.trigger.type) : "auto"),
          trigger: t.trigger ? (t.trigger.name ?? triggerDescriptor?.displayName ?? t.trigger.type) : undefined,
          hasCondition: !!t.condition?.expression?.value,
          readOnly,
          isSelf: t.from === t.to,
          warning: issues.find((i) => i.transitionIndex === index && i.severity === "warning")?.message,
        },
      };
    });
  }, [sm, selectedIds, readOnly, registry, issues]);

  const smId = sm?.id;
  const nodesInitialized = useNodesInitialized();
  useEffect(() => {
    if (!nodesInitialized) return;
    const t = setTimeout(() => flow.fitView({ padding: 0.25, duration: 200, maxZoom: 1 }), 30);
    return () => clearTimeout(t);
  }, [smId, nodesInitialized, flow]);

  useEffect(() => {
    if (!connectFrom) return;
    const onKey = (e: globalThis.KeyboardEvent) => {
      if (e.key === "Escape") store.getState().startConnect(undefined);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [connectFrom, store]);

  const onNodesChange = useCallback(
    (changes: NodeChange<StateFlowNode>[]) => {
      const s = store.getState();
      let selection: string[] | undefined;
      for (const change of changes) {
        if (change.type === "position" && change.position && !readOnly) {
          s.moveState(selectionId.stateName(change.id), { x: Math.round(change.position.x), y: Math.round(change.position.y) }, { history: false });
        } else if (change.type === "remove" && !readOnly) {
          s.removeStates([selectionId.stateName(change.id)]);
        } else if (change.type === "select") {
          selection = selection ?? s.selectedIds.filter((id) => !selectionId.isState(id));
          if (change.selected) selection.push(change.id);
          else selection = selection.filter((id) => id !== change.id);
        }
      }
      if (selection) s.select(selection);
    },
    [store, readOnly],
  );

  const onConnect = useCallback(
    (c: FlowConnection) => {
      if (readOnly || !c.source || !c.target) return;
      store.getState().addTransition({ from: selectionId.stateName(c.source), to: selectionId.stateName(c.target) });
    },
    [store, readOnly],
  );

  const addState = useCallback(() => {
    const rect = wrapperRef.current?.getBoundingClientRect();
    const center = flow.screenToFlowPosition({ x: (rect?.left ?? 0) + (rect?.width ?? 800) / 2, y: (rect?.top ?? 0) + (rect?.height ?? 600) / 2 });
    // New states line up to the right of the right-most one so they never overlap.
    const existing = flow.getNodes();
    const rightMost = existing.reduce((max, n) => Math.max(max, n.position.x + (n.measured?.width ?? 220)), Number.NEGATIVE_INFINITY);
    const position = Number.isFinite(rightMost) ? { x: Math.round(rightMost + 90), y: Math.round(existing[0]?.position.y ?? center.y - 60) } : { x: Math.round(center.x - 110), y: Math.round(center.y - 60) };
    const name = store.getState().addState(undefined, position);
    setTimeout(() => store.getState().startRename(selectionId.state(name)), 50);
  }, [flow, store]);

  const runAutoLayout = useCallback(() => {
    if (!sm) return;
    const measured = new Map(
      flow.getNodes().map((n) => {
        const m = flow.getInternalNode(n.id)?.measured;
        return [n.id, { width: m?.width ?? 220, height: m?.height ?? 120 }];
      }),
    );
    const { positions } = layoutGraph(
      sm.states.map((st) => ({ id: selectionId.state(st.name), ...(measured.get(selectionId.state(st.name)) ?? { width: 220, height: 120 }) })),
      sm.transitions.map((t) => ({ source: selectionId.state(t.from), target: selectionId.state(t.to) })),
    );
    const s = store.getState();
    const before = s.root;
    for (const st of sm.states) {
      const p = positions[selectionId.state(st.name)];
      if (p) s.moveState(st.name, p, { history: false });
    }
    store.setState((x) => ({ past: [...x.past, before], future: [] }));
    setTimeout(() => flow.fitView({ padding: 0.25, duration: 200, maxZoom: 1 }), 30);
  }, [flow, store, sm]);

  const onKeyDown = useCallback(
    (e: KeyboardEvent) => {
      const target = e.target as HTMLElement;
      if (target.closest("input, textarea, [contenteditable=true], .cm-editor")) return;
      const mod = e.metaKey || e.ctrlKey;
      const s = store.getState();
      if (mod && e.key.toLowerCase() === "z") {
        e.preventDefault();
        if (e.shiftKey) s.redo();
        else s.undo();
      } else if (mod && e.key.toLowerCase() === "y") {
        e.preventDefault();
        s.redo();
      } else if (!readOnly && !mod && e.key.toLowerCase() === "s" && !e.shiftKey) {
        e.preventDefault();
        addState();
      } else if (!readOnly && e.key === "F2" && s.selectedIds.length === 1 && selectionId.isState(s.selectedIds[0])) {
        e.preventDefault();
        s.startRename(s.selectedIds[0]);
      } else if (!readOnly && (e.key === "Delete" || e.key === "Backspace")) {
        const ids = s.selectedIds;
        const states = ids.filter(selectionId.isState).map(selectionId.stateName);
        const transitions = ids.filter(selectionId.isTransition).map(selectionId.transitionIndex);
        if (transitions.length) s.removeTransitions(transitions);
        if (states.length) s.removeStates(states);
      }
    },
    [store, readOnly, addState],
  );

  if (!sm) return null;
  const isEmpty = sm.states.length === 0;

  return (
    <div ref={wrapperRef} className={cn("relative h-full w-full min-h-[320px] outline-none", className)} tabIndex={0} onKeyDown={onKeyDown}>
      <ReactFlow<StateFlowNode, TransitionFlowEdge>
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        onNodesChange={onNodesChange}
        onEdgesChange={(changes) => {
          const removed = changes.filter((c) => c.type === "remove").map((c) => selectionId.transitionIndex(c.id));
          if (removed.length && !readOnly) store.getState().removeTransitions(removed);
          // Edge selection is owned by React Flow; mirror it into the store one way (never back).
          const picked = changes.flatMap((c) => (c.type === "select" && c.selected ? [c.id] : []));
          if (picked.length) store.getState().select(picked);
        }}
        onConnect={onConnect}
        onNodeDragStart={() => (dragSnapshot.current = store.getState().root)}
        onNodeDragStop={() => {
          const before = dragSnapshot.current;
          dragSnapshot.current = null;
          if (before && before !== store.getState().root) store.setState((s) => ({ past: [...s.past, before], future: [] }));
        }}
        onPaneClick={() => store.getState().select([])}
        onDoubleClick={(e) => {
          if ((e.target as HTMLElement).closest(".react-flow__pane") && !readOnly) addState();
        }}
        nodesConnectable={!readOnly}
        nodesDraggable={!readOnly}
        elementsSelectable
        deleteKeyCode={null}
        panOnDrag
        zoomOnDoubleClick={false}
        minZoom={0.2}
        maxZoom={2.5}
        fitView
        fitViewOptions={{ padding: 0.25, maxZoom: 1 }}
        defaultEdgeOptions={{ type: "transition" }}
        proOptions={{ hideAttribution: true }}
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} />
        {minimap && <MiniMap pannable zoomable position="bottom-right" className="!bg-background/80" />}
        <Panel position="bottom-left" className="flex items-center gap-0.5 rounded-lg border bg-card/95 p-0.5 shadow-sm backdrop-blur">
          <IconButton label="Zoom out" onClick={() => flow.zoomOut({ duration: 150 })}>
            <Minus />
          </IconButton>
          <ZoomLabel onClick={() => flow.zoomTo(1, { duration: 150 })} />
          <IconButton label="Zoom in" onClick={() => flow.zoomIn({ duration: 150 })}>
            <Plus />
          </IconButton>
          <span className="mx-0.5 h-4 w-px bg-border" />
          <IconButton label="Zoom to fit" onClick={() => flow.fitView({ padding: 0.25, duration: 200, maxZoom: 1 })}>
            <Maximize />
          </IconButton>
          {onToggleMinimap && (
            <IconButton label={minimap ? "Hide mini-map" : "Show mini-map"} onClick={onToggleMinimap} className={minimap ? "text-foreground" : "text-muted-foreground"}>
              <MapIcon />
            </IconButton>
          )}
        </Panel>
        <Panel position="top-left" className="flex items-center gap-1">
          {onOpenToolbox && (
            <IconButton label="Show toolbox" onClick={onOpenToolbox} className="rounded-lg border bg-card/95 shadow-sm backdrop-blur">
              <PanelLeftOpen />
            </IconButton>
          )}
          <DesignerBreadcrumb />
          <span className="rounded-full border bg-card/95 px-2 py-0.5 text-[11px] text-muted-foreground shadow-sm">State machine</span>
        </Panel>
        {connectFrom && (
          <Panel position="top-center" className="flex items-center gap-2 rounded-full border border-status-success/40 bg-card/95 px-3 py-1 text-xs shadow-sm backdrop-blur">
            <span className="port-dot bg-status-success ring-0" />
            Click the target state of the new transition
            <Button variant="ghost" size="icon-xs" aria-label="Cancel" onClick={() => store.getState().startConnect(undefined)}>
              <X />
            </Button>
          </Panel>
        )}
        {isEmpty && !readOnly && (
          <Panel position="top-center" className="pointer-events-none mt-24">
            <div className="pointer-events-auto flex max-w-sm flex-col items-center gap-3 rounded-xl border border-dashed bg-card/95 p-6 text-center shadow-sm">
              <span className="flex size-12 items-center justify-center rounded-xl bg-accent text-primary">
                <MousePointerClick className="size-6" />
              </span>
              <div className="text-sm font-semibold">Add the first state</div>
              <p className="text-xs text-muted-foreground">
                The first state becomes the initial state. Drag from a state&apos;s port to another state to add a transition, then give it a trigger, a condition or an action.
              </p>
              <Button size="sm" onClick={addState}>
                <Plus /> Add state
              </Button>
            </div>
          </Panel>
        )}
        <Panel position="top-right" className="flex items-center gap-1 rounded-lg border bg-card/95 p-1 shadow-sm backdrop-blur">
          {!readOnly && (
            <>
              <Button size="sm" variant="ghost" className="pr-2.5" onClick={addState}>
                <Plus /> State
                <kbd className="ml-1 rounded border bg-muted px-1 text-[10px] font-mono text-muted-foreground">S</kbd>
              </Button>
              <span className="mx-0.5 h-5 w-px bg-border" />
              <IconButton label="Undo (⌘Z)" onClick={() => store.getState().undo()} disabled={!canUndo}>
                <Undo2 />
              </IconButton>
              <IconButton label="Redo (⇧⌘Z)" onClick={() => store.getState().redo()} disabled={!canRedo}>
                <Redo2 />
              </IconButton>
              <IconButton label="Auto layout" onClick={runAutoLayout}>
                <LayoutGrid />
              </IconButton>
            </>
          )}
        </Panel>
      </ReactFlow>
      <QuickAddMenu />
    </div>
  );
}

function ZoomLabel({ onClick }: { onClick: () => void }) {
  const { zoom } = useViewport();
  return (
    <button type="button" onClick={onClick} title="Reset zoom to 100%" className="w-11 rounded text-center text-[11px] tabular-nums text-muted-foreground hover:bg-accent hover:text-foreground">
      {Math.round(zoom * 100)}%
    </button>
  );
}
