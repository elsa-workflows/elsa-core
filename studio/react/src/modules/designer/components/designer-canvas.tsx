"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type DragEvent, type KeyboardEvent } from "react";
import {
  Background,
  BackgroundVariant,
  MiniMap,
  Panel,
  ReactFlow,
  useNodesInitialized,
  useReactFlow,
  ViewportPortal,
  type Connection as FlowConnection,
  type EdgeChange,
  MarkerType,
  type IsValidConnection,
  type NodeChange,
  type OnConnect,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { Database, LayoutGrid, Map as MapIcon, Maximize, Minus, MousePointerClick, PanelLeftOpen, Plus, Redo2, Undo2, X } from "lucide-react";
import { useViewport } from "@xyflow/react";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import { DEFAULT_IN_PORT } from "@/lib/api/types/activities";
import { useDesigner, useDesignerState } from "../context";
import { connectionKey, resolveContainer } from "../model/flowchart-ops";
import { autoLayout } from "../model/layout";
import { computePorts } from "../model/ports";
import { ActivityNode, type ActivityFlowNode } from "./activity-node";
import { readActivityDragData } from "./activity-toolbox";
import { ConnectionEdge, type ConnectionFlowEdge } from "./connection-edge";
import { ContextBoxNode } from "./context-box-node";
import { ContextCallEdge } from "./context-call-edge";
import { ContextWorkflowNode } from "./context-workflow-node";
import { DataEdge, type DataFlowEdge } from "./data-edge";
import { isDecorationId, type CanvasDecorations, type DecorationEdge, type DecorationNode } from "./decorations";
import { DesignerBreadcrumb } from "./designer-breadcrumb";
import { QuickAddMenu } from "./quick-add-menu";
import { issuesByActivity, validateWorkflow } from "../model/validation";
import { isFlowchart, type Flowchart } from "@/lib/api/types/activities";
import { emptyFlowchart } from "../model/flowchart-ops";
import { snapNode, type HelperLines } from "../model/snapping";

const nodeTypes = { activity: ActivityNode, contextBox: ContextBoxNode, contextWorkflow: ContextWorkflowNode };
const edgeTypes = { connection: ConnectionEdge, data: DataEdge, contextCall: ContextCallEdge };
type CanvasEdge = ConnectionFlowEdge | DataFlowEdge | DecorationEdge;
type CanvasNode = ActivityFlowNode | DecorationNode;
const NO_DECORATIONS: CanvasDecorations = { nodes: [], edges: [] };

export interface DesignerCanvasProps {
  className?: string;
  /** Shows the mini-map (useful in small panes to hide it). */
  minimap?: boolean;
  onToggleMinimap?: () => void;
  /** When the toolbox is hidden, offers a button to reopen it. */
  onOpenToolbox?: () => void;
  /** Extra toolbar buttons rendered next to the built-in ones. */
  toolbar?: React.ReactNode;
  /** Called when the user double-clicks a node (instance viewer opens details). */
  onNodeDoubleClick?: (activityId: string) => void;
  /** Node ids whose outgoing edges should be highlighted as traversed (instance viewer). */
  traversedActivityIds?: Set<string>;
  /** View-only nodes and edges drawn next to the activities (call-context box and external workflows). */
  decorations?: CanvasDecorations;
}

/**
 * React Flow canvas bound to the designer store. The store is the single source of truth: nodes and
 * edges are derived from the current container, and every interaction is written back as a
 * flowchart mutation so undo/redo and serialization stay trivial.
 */
export function DesignerCanvas({ className, minimap = true, onToggleMinimap, onOpenToolbox, toolbar, onNodeDoubleClick, traversedActivityIds, decorations = NO_DECORATIONS }: DesignerCanvasProps) {
  const { store, registry, stats, dataFlow } = useDesigner();
  const showDataFlow = useDesignerState((s) => s.showDataFlow);
  const dataHighlight = useDesignerState((s) => s.dataHighlight);
  const root = useDesignerState((s) => s.root);
  const path = useDesignerState((s) => s.path);
  const selectedIds = useDesignerState((s) => s.selectedIds);
  const readOnly = useDesignerState((s) => s.readOnly);
  const canUndo = useDesignerState((s) => s.past.length > 0);
  const canRedo = useDesignerState((s) => s.future.length > 0);
  const flow = useReactFlow<CanvasNode, CanvasEdge>();
  const wrapperRef = useRef<HTMLDivElement>(null);
  const dragSnapshot = useRef<typeof root>(null);
  const [helperLines, setHelperLines] = useState<HelperLines>({});

  const resolved = useMemo(() => resolveContainer(root, path), [root, path]);
  const container = useMemo<Flowchart>(() => (isFlowchart(resolved) ? resolved : emptyFlowchart("__none__")), [resolved]);
  const issues = useMemo(() => (readOnly ? new Map() : issuesByActivity(validateWorkflow(root, registry))), [root, registry, readOnly]);

  const nodes = useMemo<ActivityFlowNode[]>(
    () =>
      container.activities.map((activity) => {
        const descriptor = registry.get(activity.type, activity.version);
        const size = activity.metadata?.designer?.size;
        return {
          id: activity.id,
          type: "activity",
          position: activity.metadata?.designer?.position ?? { x: 0, y: 0 },
          selected: selectedIds.includes(activity.id),
          draggable: !readOnly,
          ...(size ? { width: size.width, height: size.height } : {}),
          data: {
            activity,
            descriptor,
            ports: computePorts(activity, descriptor),
            stats: stats?.get(activity.nodeId ?? activity.id),
            readOnly,
            issues: issues.get(activity.id),
            dataSummary: showDataFlow || dataHighlight ? dataFlow?.byActivity.get(activity.id) : undefined,
            dataRole: dataHighlight ? dataRoleOf(dataFlow, dataHighlight, activity.id) : undefined,
          },
        };
      }),
    [container, registry, selectedIds, readOnly, stats, issues, showDataFlow, dataHighlight, dataFlow],
  );

  const dataEdges = useMemo<DataFlowEdge[]>(() => {
    if (!dataFlow || (!showDataFlow && !dataHighlight)) return [];
    const ids = new Set(container.activities.map((a) => a.id));
    const result: DataFlowEdge[] = [];
    for (const usage of dataFlow.list) {
      const key = `${usage.kind}:${usage.key}`;
      if (dataHighlight && dataHighlight !== key) continue;
      for (const w of usage.writers) {
        for (const r of usage.readers) {
          if (!ids.has(w.activityId) || !ids.has(r.activityId) || w.activityId === r.activityId) continue;
          result.push({
            id: `data:${key}:${w.activityId}->${r.activityId}`,
            type: "data",
            source: w.activityId,
            sourceHandle: "__data-out",
            target: r.activityId,
            targetHandle: "__data-in",
            selectable: false,
            focusable: false,
            zIndex: 1,
            data: { label: usage.name, kind: usage.kind, highlighted: dataHighlight === key, dimmed: false },
          });
        }
      }
    }
    return result;
  }, [dataFlow, showDataFlow, dataHighlight, container]);

  const edges = useMemo<ConnectionFlowEdge[]>(
    () =>
      container.connections.map((c) => {
        const source = container.activities.find((a) => a.id === c.source.activity);
        const descriptor = source ? registry.get(source.type, source.version) : undefined;
        const ports = source ? computePorts(source, descriptor) : undefined;
        const port = ports?.outPorts.find((p) => p.name === c.source.port);
        const traversed = !!traversedActivityIds?.has(c.source.activity) && !!traversedActivityIds?.has(c.target.activity);
        return {
          id: connectionKey(c),
          type: "connection",
          className: traversed ? "traversed" : undefined,
          markerEnd: { type: MarkerType.ArrowClosed, width: 14, height: 14, color: traversed ? "var(--status-success)" : "var(--xy-edge-stroke)" },
          source: c.source.activity,
          sourceHandle: c.source.port,
          target: c.target.activity,
          targetHandle: c.target.port || DEFAULT_IN_PORT,
          data: {
            connection: c,
            label: c.source.port === "Done" && (ports?.outPorts.length ?? 1) <= 1 ? undefined : (port?.displayName ?? c.source.port),
            readOnly,
            traversed,
          },
        };
      }),
    [container, registry, readOnly, traversedActivityIds],
  );

  // Fit the view once nodes are measured, whenever the container changes (drill-in / breadcrumb) and
  // when decorations appear or grow (call context toggled on, graph finished loading).
  const containerId = container.id;
  const nodesInitialized = useNodesInitialized();
  const decorationCount = decorations.nodes.length;
  useEffect(() => {
    if (!nodesInitialized) return;
    const t = setTimeout(() => flow.fitView({ padding: 0.2, duration: 200, maxZoom: 1 }), 30);
    return () => clearTimeout(t);
  }, [containerId, nodesInitialized, flow, decorationCount]);

  const onNodesChange = useCallback(
    (allChanges: NodeChange<CanvasNode>[]) => {
      // Decorations are not part of the workflow: nothing they report is written back to the store.
      const changes = allChanges.filter((c) => !("id" in c) || !isDecorationId(c.id)) as NodeChange<ActivityFlowNode>[];
      if (changes.length === 0) return;
      const state = store.getState();
      const moved: Record<string, { x: number; y: number }> = {};
      const removed: string[] = [];
      let selection: string[] | undefined;
      // Snap a single dragged node to neighbouring ports/edges and show helper lines. The final change
      // React Flow emits on drop (dragging: false) carries the raw pointer position, so it is snapped too.
      const positionChanges = changes.filter((c): c is Extract<NodeChange<ActivityFlowNode>, { type: "position" }> => c.type === "position" && !!c.position);
      if (positionChanges.length === 1 && !readOnly) {
        const change = positionChanges[0];
        const dragged = flow.getInternalNode(change.id);
        if (dragged && change.position) {
          const others = flow.getNodes().filter((n) => n.id !== change.id && !n.selected && !isDecorationId(n.id)).map((n) => flow.getInternalNode(n.id)!).filter(Boolean);
          const result = snapNode(dragged, change.position, others);
          change.position = result.position;
          setHelperLines(change.dragging ? result.lines : {});
        }
      } else if (positionChanges.length !== 1 && changes.some((c) => c.type === "position")) {
        setHelperLines({});
      }
      for (const change of changes) {
        switch (change.type) {
          case "position":
            if (change.position && !readOnly) moved[change.id] = { x: Math.round(change.position.x), y: Math.round(change.position.y) };
            break;
          case "dimensions":
            if (change.dimensions && change.dimensions.width > 0 && !readOnly) state.setActivitySize(change.id, change.dimensions);
            break;
          case "remove":
            if (!readOnly) removed.push(change.id);
            break;
          case "select":
            selection = selection ?? [...state.selectedIds];
            if (change.selected && !selection.includes(change.id)) selection.push(change.id);
            if (!change.selected) selection = selection.filter((id) => id !== change.id);
            break;
        }
      }
      if (Object.keys(moved).length) state.moveActivities(moved, { history: false });
      if (removed.length) state.removeActivities(removed);
      if (selection) state.select(selection);
    },
    [store, readOnly, flow],
  );

  const onEdgesChange = useCallback(
    (changes: EdgeChange<CanvasEdge>[]) => {
      if (readOnly) return;
      const byId = new Map(edges.map((e) => [e.id, e]));
      const removed = changes.filter((c) => c.type === "remove").map((c) => byId.get(c.id)?.data?.connection).filter((c): c is NonNullable<typeof c> => !!c);
      if (removed.length) store.getState().removeConnections(removed);
    },
    [edges, store, readOnly],
  );

  const onConnect = useCallback<OnConnect>(
    (c: FlowConnection) => {
      if (readOnly || !c.source || !c.target) return;
      store.getState().addConnection({
        source: { activity: c.source, port: c.sourceHandle ?? "Done" },
        target: { activity: c.target, port: c.targetHandle ?? DEFAULT_IN_PORT },
      });
    },
    [store, readOnly],
  );

  const isValidConnection = useCallback<IsValidConnection<CanvasEdge>>(
    (c) => !!c.source && !!c.target && (c.targetHandle ?? DEFAULT_IN_PORT) === DEFAULT_IN_PORT && !!c.sourceHandle && !c.sourceHandle.startsWith("__"),
    [],
  );

  const onDrop = useCallback(
    (e: DragEvent) => {
      e.preventDefault();
      if (readOnly) return;
      const data = readActivityDragData(e);
      if (!data) return;
      const descriptor = registry.get(data.typeName, data.version);
      if (!descriptor) return;
      const position = flow.screenToFlowPosition({ x: e.clientX, y: e.clientY });
      store.getState().addActivity(descriptor, { x: Math.round(position.x), y: Math.round(position.y) });
    },
    [flow, registry, store, readOnly],
  );

  const onDragOver = useCallback((e: DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = "copy";
  }, []);

  const runAutoLayout = useCallback(() => {
    // Measurements live on React Flow's internal node records, not on the nodes passed in.
    const measured = new Map(
      flow.getNodes().filter((n) => !isDecorationId(n.id)).map((n) => {
        const m = flow.getInternalNode(n.id)?.measured;
        return [n.id, { width: m?.width ?? n.width ?? 220, height: m?.height ?? n.height ?? 56 }];
      }),
    );
    const result = autoLayout(
      container,
      measured,
      (a) => computePorts(a, registry.get(a.type, a.version)).outPorts,
    );
    store.getState().moveActivities(result.positions);
    store.getState().setConnectionVertices(result.vertices, { history: false });
    setTimeout(() => flow.fitView({ padding: 0.2, duration: 200, maxZoom: 1 }), 30);
  }, [flow, store, container, registry]);

  const openQuickAddAt = useCallback(
    (clientX: number, clientY: number) => {
      if (readOnly) return;
      const flowPosition = flow.screenToFlowPosition({ x: clientX, y: clientY });
      store.getState().openQuickAdd({ screen: { x: clientX, y: clientY }, flowPosition: { x: flowPosition.x - 110, y: flowPosition.y - 28 } });
    },
    [flow, store, readOnly],
  );

  const onKeyDown = useCallback(
    (e: KeyboardEvent) => {
      const target = e.target as HTMLElement;
      if (target.closest("input, textarea, [contenteditable=true], .cm-editor")) return;
      const mod = e.metaKey || e.ctrlKey;
      const s = store.getState();
      if (!readOnly && e.key === "F2" && s.selectedIds.length === 1) {
        e.preventDefault();
        s.startRename(s.selectedIds[0]);
      } else if (!readOnly && !mod && e.key.toLowerCase() === "a" && !e.shiftKey) {
        e.preventDefault();
        const rect = wrapperRef.current?.getBoundingClientRect();
        const selected = s.selectedActivity();
        if (selected) {
          const descriptor = registry.get(selected.type, selected.version);
          const port = computePorts(selected, descriptor).outPorts[0];
          if (port) {
            s.openQuickAdd({ screen: { x: (rect?.left ?? 0) + (rect?.width ?? 0) / 2 - 170, y: (rect?.top ?? 0) + 80 }, source: { activityId: selected.id, port: port.name } });
            return;
          }
        }
        if (rect) openQuickAddAt(rect.left + rect.width / 2, rect.top + rect.height / 2);
      } else if (mod && e.key.toLowerCase() === "z") {
        e.preventDefault();
        if (e.shiftKey) s.redo();
        else s.undo();
      } else if (mod && e.key.toLowerCase() === "y") {
        e.preventDefault();
        s.redo();
      } else if (!readOnly && mod && e.key.toLowerCase() === "c") {
        s.copySelection();
      } else if (!readOnly && mod && e.key.toLowerCase() === "x") {
        e.preventDefault();
        s.cut();
      } else if (!readOnly && mod && e.key.toLowerCase() === "v") {
        e.preventDefault();
        s.paste();
      } else if (!readOnly && mod && e.key.toLowerCase() === "d") {
        e.preventDefault();
        if (s.selectedIds.length === 1) s.duplicate(s.selectedIds[0]);
      } else if (mod && e.key.toLowerCase() === "a") {
        e.preventDefault();
        s.select(s.children().map((a) => a.id));
      }
    },
    [store, readOnly, registry, openQuickAddAt],
  );

  const isEmpty = container.activities.length === 0;
  const connectFrom = useDesignerState((s) => s.connectFrom);
  useEffect(() => {
    if (!connectFrom) return;
    const onKey = (e: globalThis.KeyboardEvent) => {
      if (e.key === "Escape") store.getState().startConnect(undefined);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [connectFrom, store]);

  // Bring a quick-added activity into view when it lands outside the visible area.
  const lastAddedId = useDesignerState((s) => s.lastAddedId);
  const lastAddedSource = useDesignerState((s) => s.lastAddedSource);
  useEffect(() => {
    if (!lastAddedId) return;
    const t = setTimeout(() => {
      const node = flow.getNode(lastAddedId);
      const el = wrapperRef.current;
      if (!node || !el) return;
      // Level the new step's in-port with the source port it was appended from (straight connection).
      if (lastAddedSource) {
        const source = flow.getInternalNode(lastAddedSource.activityId);
        const added = flow.getInternalNode(lastAddedId);
        const out = source?.internals.handleBounds?.source?.find((h) => h.id === lastAddedSource.port);
        const inp = added?.internals.handleBounds?.target?.find((h) => !h.id?.startsWith("__"));
        if (source && out && inp) {
          let targetY = source.internals.positionAbsolute.y + out.y + out.height / 2 - (inp.y + inp.height / 2);
          // Keep the levelled spot only if it is free; otherwise drop below whatever is in the way.
          const w = added?.measured.width ?? 220;
          const h = added?.measured.height ?? 60;
          const GAP = 24;
          const others = flow.getNodes().filter((n) => n.id !== lastAddedId && !isDecorationId(n.id));
          for (let guard = 0; guard < 20; guard++) {
            const hit = others.find((o) => {
              const ow = o.measured?.width ?? 220;
              const oh = o.measured?.height ?? 60;
              return node.position.x < o.position.x + ow + GAP && node.position.x + w + GAP > o.position.x && targetY < o.position.y + oh + GAP && targetY + h + GAP > o.position.y;
            });
            if (!hit) break;
            targetY = hit.position.y + (hit.measured?.height ?? 60) + GAP;
          }
          if (Math.abs(targetY - node.position.y) > 0.5) store.getState().moveActivities({ [lastAddedId]: { x: node.position.x, y: Math.round(targetY) } }, { history: false });
        }
      }
      const { x, y, zoom } = flow.getViewport();
      const w = node.measured?.width ?? 220;
      const h = node.measured?.height ?? 60;
      const left = node.position.x * zoom + x;
      const top = node.position.y * zoom + y;
      const inside = left >= 0 && top >= 0 && left + w * zoom <= el.clientWidth && top + h * zoom <= el.clientHeight;
      if (!inside) void flow.setCenter(node.position.x + w / 2, node.position.y + h / 2, { zoom, duration: 300 });
      store.setState({ lastAddedId: undefined, lastAddedSource: undefined });
    }, 60);
    return () => clearTimeout(t);
  }, [lastAddedId, lastAddedSource, flow, store]);

  return (
    <div ref={wrapperRef} className={cn("relative h-full w-full min-h-[320px] outline-none", className)} tabIndex={0} onKeyDown={onKeyDown}>
      <ReactFlow<CanvasNode, CanvasEdge>
        nodes={decorations.nodes.length ? [...decorations.nodes, ...nodes] : nodes}
        edges={[...edges, ...dataEdges, ...decorations.edges]}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        isValidConnection={isValidConnection}
        onDrop={onDrop}
        onDragOver={onDragOver}
        onNodeDragStart={() => (dragSnapshot.current = store.getState().root)}
        onNodeDragStop={() => {
          setHelperLines({});
          const before = dragSnapshot.current;
          dragSnapshot.current = null;
          if (before && before !== store.getState().root) store.setState((s) => ({ past: [...s.past, before], future: [] }));
        }}
        onNodeDoubleClick={(_, node) => {
          if (!isDecorationId(node.id)) onNodeDoubleClick?.(node.id);
        }}
        onPaneClick={() => store.getState().select([])}
        onDoubleClick={(e) => {
          if ((e.target as HTMLElement).closest(".react-flow__pane")) openQuickAddAt(e.clientX, e.clientY);
        }}
        nodesConnectable={!readOnly}
        nodesDraggable={!readOnly}
        elementsSelectable
        deleteKeyCode={readOnly ? null : ["Delete", "Backspace"]}
        multiSelectionKeyCode={["Shift", "Meta", "Control"]}
        selectionOnDrag={false}
        panOnDrag
        zoomOnDoubleClick={false}
        minZoom={0.2}
        maxZoom={2.5}
        fitView
        fitViewOptions={{ padding: 0.2, maxZoom: 1 }}
        defaultEdgeOptions={{ type: "connection" }}
        proOptions={{ hideAttribution: true }}
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} />
        {(helperLines.horizontal || helperLines.vertical) && (
          <ViewportPortal>
            {helperLines.horizontal && (
              <div
                className="pointer-events-none absolute border-t border-dashed border-primary"
                style={{ left: helperLines.horizontal.from, top: helperLines.horizontal.y, width: helperLines.horizontal.to - helperLines.horizontal.from, zIndex: 2 }}
              />
            )}
            {helperLines.vertical && (
              <div
                className="pointer-events-none absolute border-l border-dashed border-primary"
                style={{ left: helperLines.vertical.x, top: helperLines.vertical.from, height: helperLines.vertical.to - helperLines.vertical.from, zIndex: 2 }}
              />
            )}
          </ViewportPortal>
        )}
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
          <IconButton label="Zoom to fit" onClick={() => flow.fitView({ padding: 0.2, duration: 200, maxZoom: 1 })}>
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
        </Panel>
        {connectFrom && (
          <Panel position="top-center" className="flex items-center gap-2 rounded-full border border-status-success/40 bg-card/95 px-3 py-1 text-xs shadow-sm backdrop-blur">
            <span className="port-dot bg-status-success ring-0" />
            Click the step to connect <span className="font-medium">{connectFrom.port}</span> to
            <Button variant="ghost" size="icon-xs" aria-label="Cancel connecting" onClick={() => store.getState().startConnect(undefined)}>
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
              <div className="text-sm font-semibold">Start with a trigger or an action</div>
              <p className="text-xs text-muted-foreground">
                Add an activity and keep going with the <span className="font-medium text-foreground">+</span> next to each step. You can also drag from the toolbox or double-click the canvas.
              </p>
              <Button
                size="sm"
                onClick={(e) => {
                  const rect = wrapperRef.current?.getBoundingClientRect();
                  openQuickAddAt(rect ? rect.left + rect.width / 2 : e.clientX, rect ? rect.top + rect.height / 2 : e.clientY);
                }}
              >
                <Plus /> Add activity
              </Button>
            </div>
          </Panel>
        )}
        <Panel position="top-right" className="flex items-center gap-1 rounded-lg border bg-card/95 p-1 shadow-sm backdrop-blur">
          {!readOnly && (
            <>
              <Button
                size="sm"
                variant="ghost"
                className="pr-2.5"
                onClick={() => {
                  const rect = wrapperRef.current?.getBoundingClientRect();
                  if (rect) openQuickAddAt(rect.left + rect.width / 2, rect.top + rect.height / 2);
                }}
              >
                <Plus /> Add
                <kbd className="ml-1 rounded border bg-muted px-1 text-[10px] font-mono text-muted-foreground">A</kbd>
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
          {dataFlow && (
            <IconButton
              label={showDataFlow ? "Hide data flow" : "Show data flow (variables, inputs, outputs)"}
              onClick={() => store.getState().setShowDataFlow(!showDataFlow)}
              className={showDataFlow ? "bg-accent text-primary" : undefined}
            >
              <Database />
            </IconButton>
          )}
          {readOnly && (
            <IconButton label="Zoom to fit" onClick={() => flow.fitView({ padding: 0.2, duration: 200, maxZoom: 1 })}>
              <Maximize />
            </IconButton>
          )}
          {toolbar}
        </Panel>
      </ReactFlow>
      <QuickAddMenu />
    </div>
  );
}

function dataRoleOf(index: import("../model/data-flow").DataFlowIndex | undefined, key: string, activityId: string): "writer" | "reader" | "both" | undefined {
  const usage = index?.byKey.get(key);
  if (!usage) return undefined;
  const w = usage.writers.some((u) => u.activityId === activityId);
  const r = usage.readers.some((u) => u.activityId === activityId);
  return w && r ? "both" : w ? "writer" : r ? "reader" : undefined;
}

function ZoomLabel({ onClick }: { onClick: () => void }) {
  const { zoom } = useViewport();
  return (
    <button type="button" onClick={onClick} title="Reset zoom to 100%" className="w-11 rounded text-center text-[11px] tabular-nums text-muted-foreground hover:bg-accent hover:text-foreground">
      {Math.round(zoom * 100)}%
    </button>
  );
}

export function IconButton({ label, children, ...props }: React.ComponentProps<typeof Button> & { label: string }) {
  return (
    <Tooltip>
      <TooltipTrigger render={<Button variant="ghost" size="icon-sm" aria-label={label} {...props} />}>{children}</TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}
