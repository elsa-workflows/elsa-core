import type { Edge, Node } from "@xyflow/react";

/**
 * View-only decorations a page can draw on the designer canvas next to the real activities: a
 * grouping box around the current workflow and external nodes/edges (used by the call-context view).
 * They are never written to the workflow definition and cannot be selected, dragged or connected.
 * Every decoration id starts with `DECORATION_PREFIX` so the canvas can ignore React Flow changes for them.
 */
export const DECORATION_PREFIX = "ctx:";
export const CONTEXT_BOX_ID = `${DECORATION_PREFIX}box`;
/** Hidden source handle on every activity node so a decoration edge can leave a specific step. */
export const ACTIVITY_CONTEXT_HANDLE = "__context-out";

export type DecorationNodeKind = "workflow" | "missing" | "unresolved" | "truncated" | "pending";

export interface ContextBoxData extends Record<string, unknown> {
  title: string;
  subtitle?: string;
}

export interface ContextWorkflowData extends Record<string, unknown> {
  kind: DecorationNodeKind;
  name: string;
  subtitle?: string;
  side: "incoming" | "outgoing";
  /** Drill-in action; absent for placeholders. */
  onOpen?: () => void;
  /** Expands a collapsed branch; present on "truncated" placeholders that stand for loaded nodes. */
  onExpand?: () => void;
}

export interface ContextCallEdgeData extends Record<string, unknown> {
  label: string;
  /** Closes a cycle: drawn dashed in the warning colour. */
  back: boolean;
}

export type ContextBoxNode = Node<ContextBoxData, "contextBox">;
export type ContextWorkflowNode = Node<ContextWorkflowData, "contextWorkflow">;
export type DecorationNode = ContextBoxNode | ContextWorkflowNode;
export type DecorationEdge = Edge<ContextCallEdgeData, "contextCall">;

export interface CanvasDecorations {
  nodes: DecorationNode[];
  edges: DecorationEdge[];
}

export function isDecorationId(id: string) {
  return id.startsWith(DECORATION_PREFIX);
}
