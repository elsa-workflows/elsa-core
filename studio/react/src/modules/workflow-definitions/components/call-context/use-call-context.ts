"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import type { Activity } from "@/lib/api/types/activities";
import { isFlowchart } from "@/lib/api/types/activities";
import {
  ACTIVITY_CONTEXT_HANDLE,
  CONTEXT_BOX_ID,
  DECORATION_PREFIX,
  type CanvasDecorations,
  type DecorationEdge,
  type DecorationNode,
  type DesignerStore,
} from "@/modules/designer";
import { activitySize } from "@/modules/designer/model/display";
import { computeContextSubgraph, layoutContextNodes, useCallGraphService, useCallGraphVersion, CONTEXT_NODE_HEIGHT, CONTEXT_NODE_WIDTH, type ContextNode, type ContextSubgraph } from "../../call-graph";
import type { CallGraphWarning } from "../../call-graph/types";

const BOX_PADDING = 56;

export interface CallContextState {
  /** Decorations for the designer canvas (empty when disabled or when a nested container is open). */
  decorations: CanvasDecorations;
  status: "idle" | "loading" | "ready" | "error" | "cancelled";
  warnings: CallGraphWarning[];
  error?: unknown;
  /** True when the current canvas cannot show the context (nested container, non-flowchart root). */
  unsupported: boolean;
  counts: { incoming: number; outgoing: number };
  truncated: { incoming: boolean; outgoing: boolean };
  refresh: () => void;
  expand: (side: "incoming" | "outgoing") => void;
}

/**
 * Drives the call-context view for the workflow open in the editor.
 *
 * Builds (or reuses) the session call graph when `enabled` turns on, pushes the editor's unsaved
 * outgoing edges into it, derives the transitive subgraph, lays the external nodes out around the
 * box and turns everything into canvas decorations. Direct callees come from the in-memory root, so
 * they appear immediately while the rest of the graph loads. Rendering never starts a build: only the
 * toggle, a navigation (new `definitionId`) or `refresh` does.
 */
export function useCallContext(options: {
  enabled: boolean;
  definitionId: string;
  name: string | undefined;
  store: DesignerStore;
  storeVersion: number;
  onOpen: (definitionId: string) => void;
}): CallContextState {
  const { enabled, definitionId, name, store, storeVersion, onOpen } = options;
  const service = useCallGraphService();
  const graphVersion = useCallGraphVersion(service);
  const [expanded, setExpanded] = useState<{ incoming: number; outgoing: number }>(() => ({ incoming: service.config.collapseBeyondDepth, outgoing: service.config.collapseBeyondDepth }));

  // Build on toggle / navigation only. The effect's inputs are exactly those two signals.
  useEffect(() => {
    if (!enabled) return;
    void service.build();
    return () => service.cancel();
  }, [enabled, definitionId, service]);

  // The current workflow's outgoing edges follow the editor, including unsaved changes.
  useEffect(() => {
    if (!enabled) return;
    service.setLiveEdges(definitionId, name, store.getState().root);
  }, [enabled, definitionId, name, service, store, storeVersion]);

  const refresh = useCallback(() => {
    if (enabled) void service.build({ force: true });
  }, [enabled, service]);

  const expand = useCallback((side: "incoming" | "outgoing") => setExpanded((e) => ({ ...e, [side]: e[side] + service.config.collapseBeyondDepth })), [service.config.collapseBeyondDepth]);

  const state = store.getState();
  const atRoot = state.path.length === 0;
  const root = state.root;
  const unsupported = enabled && (!atRoot || !isFlowchart(root));

  const subgraph = useMemo<ContextSubgraph | undefined>(() => {
    if (!enabled) return undefined;
    return computeContextSubgraph(service.graph, definitionId, { maxDepth: service.config.maxDepth, maxNodes: service.config.maxNodes });
    // graphVersion is the change counter of service.graph.
  }, [enabled, definitionId, service, graphVersion]); // eslint-disable-line react-hooks/exhaustive-deps

  const decorations = useMemo<CanvasDecorations>(() => {
    if (!enabled || !subgraph || unsupported || !isFlowchart(root)) return { nodes: [], edges: [] };
    return buildDecorations(subgraph, root, name ?? definitionId, expanded, onOpen, expand);
  }, [enabled, subgraph, unsupported, root, name, definitionId, expanded, expand, onOpen]);

  const graph = service.graph;
  return {
    decorations,
    status: enabled ? graph.status : "idle",
    warnings: graph.warnings,
    error: graph.error,
    unsupported,
    counts: {
      incoming: subgraph?.nodes.filter((n) => n.side === "incoming" && n.kind === "workflow").length ?? 0,
      outgoing: subgraph?.nodes.filter((n) => n.side === "outgoing" && n.kind !== "truncated").length ?? 0,
    },
    truncated: subgraph?.truncated ?? { incoming: false, outgoing: false },
    refresh,
    expand,
  };
}

function buildDecorations(
  subgraph: ContextSubgraph,
  root: Activity & { activities: Activity[] },
  title: string,
  expanded: { incoming: number; outgoing: number },
  onOpen: (definitionId: string) => void,
  expand: (side: "incoming" | "outgoing") => void,
): CanvasDecorations {
  // The box wraps the activities currently on the canvas (their stored positions and sizes).
  const activities = root.activities;
  let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
  for (const a of activities) {
    const p = a.metadata?.designer?.position ?? { x: 0, y: 0 };
    const size = activitySize(a) ?? { width: 220, height: 56 };
    minX = Math.min(minX, p.x);
    minY = Math.min(minY, p.y);
    maxX = Math.max(maxX, p.x + size.width);
    maxY = Math.max(maxY, p.y + size.height);
  }
  if (activities.length === 0) {
    minX = 0; minY = 0; maxX = 320; maxY = 160;
  }
  const box = { x: minX - BOX_PADDING, y: minY - BOX_PADDING, width: maxX - minX + 2 * BOX_PADDING, height: maxY - minY + 2 * BOX_PADDING };

  // Collapse nodes beyond the expanded depth per side (they stay loaded; a placeholder expands them).
  const visible = new Map<string, ContextNode>();
  const collapsedParents = new Map<string, { side: "incoming" | "outgoing"; depth: number; count: number }>();
  for (const node of subgraph.nodes) {
    if (node.depth <= expanded[node.side]) visible.set(node.id, node);
  }
  for (const edge of subgraph.edges) {
    const [near, far] = edge.side === "outgoing" ? [edge.source, edge.target] : [edge.target, edge.source];
    const farNode = subgraph.nodes.find((n) => n.id === far);
    if (!farNode || visible.has(far) || far === subgraph.currentId) continue;
    const nearNode = visible.get(near) ?? (near === subgraph.currentId ? undefined : undefined);
    const anchor = near === subgraph.currentId ? subgraph.currentId : nearNode?.id;
    if (!anchor) continue;
    const key = `${farNode.side}:${anchor}`;
    const entry = collapsedParents.get(key) ?? { side: farNode.side, depth: (nearNode?.depth ?? 0) + 1, count: 0 };
    entry.count++;
    collapsedParents.set(key, entry);
  }
  const placeholders: ContextNode[] = [...collapsedParents.entries()].map(([key, c]) => ({
    id: `collapsed:${key}`,
    kind: "truncated",
    name: `${c.count} more ${c.side === "incoming" ? "caller" : "callee"}${c.count === 1 ? "" : "s"}`,
    side: c.side,
    depth: c.depth,
    hiddenCount: c.count,
  }));
  const layoutNodes = [...visible.values(), ...placeholders];
  const layoutEdges = subgraph.edges.filter((e) => (visible.has(e.source) || e.source === subgraph.currentId) && (visible.has(e.target) || e.target === subgraph.currentId));
  for (const [key, c] of collapsedParents) {
    const anchor = key.slice(key.indexOf(":") + 1);
    layoutEdges.push(c.side === "outgoing"
      ? { id: `collapsed-edge:${key}`, source: anchor, target: `collapsed:${key}`, callType: "Dispatch", activityId: "", back: false, side: "outgoing" }
      : { id: `collapsed-edge:${key}`, source: `collapsed:${key}`, target: anchor, callType: "Dispatch", activityId: "", back: false, side: "incoming" });
  }
  const positions = layoutContextNodes(layoutNodes, layoutEdges, box);

  const nodes: DecorationNode[] = [
    {
      id: CONTEXT_BOX_ID,
      type: "contextBox",
      position: { x: box.x, y: box.y },
      width: box.width,
      height: box.height,
      selectable: false,
      draggable: false,
      focusable: false,
      connectable: false,
      zIndex: -1,
      style: { pointerEvents: "none", width: box.width, height: box.height },
      data: { title, subtitle: "current workflow" },
    },
  ];
  for (const node of layoutNodes) {
    const position = positions.get(node.id) ?? { x: box.x, y: box.y };
    const isCollapsed = node.id.startsWith("collapsed:");
    nodes.push({
      id: `${DECORATION_PREFIX}node:${node.id}`,
      type: "contextWorkflow",
      position,
      width: CONTEXT_NODE_WIDTH,
      height: CONTEXT_NODE_HEIGHT,
      // React Flow only forwards pointer events to selectable or draggable nodes; the canvas discards
      // every change reported for decoration ids, so "selectable" never turns into a selection.
      selectable: true,
      draggable: false,
      connectable: false,
      data: {
        kind: node.kind,
        name: node.name,
        side: node.side,
        subtitle: subtitleOf(node),
        onOpen: node.kind === "workflow" && node.definitionId ? () => onOpen(node.definitionId!) : undefined,
        onExpand: isCollapsed ? () => expand(node.side) : undefined,
      },
    });
  }

  const onCanvas = new Set(activities.map((a) => a.id));
  const nodeId = (id: string) => (id === subgraph.currentId ? CONTEXT_BOX_ID : `${DECORATION_PREFIX}node:${id}`);
  const xOf = (id: string) => (id === subgraph.currentId ? box.x + box.width / 2 : (positions.get(id)?.x ?? 0) + CONTEXT_NODE_WIDTH / 2);
  const edges: DecorationEdge[] = layoutEdges.map((edge) => {
    const leftToRight = xOf(edge.source) <= xOf(edge.target);
    // Direct callees leave from the calling activity when it is on this canvas.
    const fromActivity = edge.source === subgraph.currentId && edge.activityId && onCanvas.has(edge.activityId) && leftToRight;
    const source = fromActivity ? edge.activityId : nodeId(edge.source);
    const sourceHandle = fromActivity ? ACTIVITY_CONTEXT_HANDLE : edge.source === subgraph.currentId ? (leftToRight ? "out" : "out-left") : leftToRight ? "out-right" : "out-left";
    const targetHandle = edge.target === subgraph.currentId ? (leftToRight ? "in" : "in-right") : leftToRight ? "in-left" : "in-right";
    return {
      id: `${DECORATION_PREFIX}edge:${edge.id}`,
      type: "contextCall",
      source,
      sourceHandle,
      target: nodeId(edge.target),
      targetHandle,
      selectable: false,
      focusable: false,
      deletable: false,
      zIndex: 0,
      data: { label: edge.activityId ? edge.callType : "…", back: edge.back },
    };
  });
  return { nodes, edges };
}

function subtitleOf(node: ContextNode): string | undefined {
  switch (node.kind) {
    case "workflow":
      return node.version !== undefined ? `v${node.version}${node.isPublished === false ? " · draft" : ""}` : undefined;
    case "missing":
      return "not found";
    case "unresolved":
      return "target set by expression";
    case "pending":
      return "loading…";
    case "truncated":
      return node.hiddenCount ? "collapsed" : "limit reached";
  }
}
