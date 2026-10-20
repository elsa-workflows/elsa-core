import type { CallType } from "./config";
import { effectiveEdges, type CallGraph } from "./types";

export type ContextSide = "incoming" | "outgoing";

export type ContextNodeKind =
  /** A workflow definition that was loaded. */
  | "workflow"
  /** The referenced definition id does not exist or could not be loaded. */
  | "missing"
  /** The call's target is an expression and cannot be resolved statically. */
  | "unresolved"
  /** Placeholder: the branch continues beyond the depth or node limit. */
  | "truncated"
  /** Referenced by an edge but not loaded yet (the graph is still building). */
  | "pending";

export interface ContextNode {
  /** Graph-unique id: the definition id for workflows, synthetic for the other kinds. */
  id: string;
  kind: ContextNodeKind;
  definitionId?: string;
  name: string;
  side: ContextSide;
  /** Distance from the current workflow in calls (1 = direct). */
  depth: number;
  version?: number;
  isPublished?: boolean;
  /** For "truncated": how many further nodes exist beyond the placeholder, when known. */
  hiddenCount?: number;
}

export interface ContextEdge {
  id: string;
  /** Definition id of the caller, or the current workflow's id. */
  source: string;
  /** Node id of the callee (definition id, or a synthetic unresolved/missing/truncated id). */
  target: string;
  callType: CallType;
  /** The calling activity, for anchoring the edge to it when it is on the canvas. */
  activityId: string;
  /** Closes a cycle (points at the current workflow or back towards it). */
  back: boolean;
  side: ContextSide;
}

export interface ContextSubgraph {
  currentId: string;
  nodes: ContextNode[];
  edges: ContextEdge[];
  truncated: { incoming: boolean; outgoing: boolean };
}

export interface SubgraphLimits {
  maxDepth: number;
  maxNodes: number;
}

/**
 * Derives the transitive callers and callees of `currentId` from the cached graph.
 *
 * Both walks are iterative breadth-first traversals with their own visited set; the current workflow
 * is visited from the start on both sides. A definition is expanded at most once per side, no matter
 * how many paths lead to it; later edges to it are still drawn, marked `back` when they point to the
 * current workflow or to a node at the same or a smaller depth (a cycle). A definition reached on both
 * sides (a cycle through the current workflow) is placed once, on the side where it is closer, and the
 * other side's edges are drawn to that single node as back edges. Depth and node limits stop a walk and
 * leave a "truncated" placeholder on the branch.
 */
export function computeContextSubgraph(graph: CallGraph, currentId: string, limits: SubgraphLimits): ContextSubgraph {
  const outgoing = walkOutgoing(graph, currentId, limits);
  const incoming = walkIncoming(graph, currentId, limits, outgoing.count);
  const nodes = new Map<string, ContextNode>();
  const edges: ContextEdge[] = [];

  // Workflows that appear on both sides are kept on the side where they are closer (tie: outgoing).
  for (const node of outgoing.nodes.values()) nodes.set(node.id, node);
  for (const node of incoming.nodes.values()) {
    const existing = nodes.get(node.id);
    if (!existing) nodes.set(node.id, node);
    else if (node.depth < existing.depth) nodes.set(node.id, node);
  }
  for (const edge of [...outgoing.edges, ...incoming.edges]) {
    const sourceSide = edge.source === currentId ? undefined : nodes.get(edge.source)?.side;
    const targetSide = edge.target === currentId ? undefined : nodes.get(edge.target)?.side;
    // An edge that crosses sides is part of a cycle through the current workflow.
    const crosses = sourceSide && targetSide && sourceSide !== targetSide;
    edges.push(crosses ? { ...edge, back: true } : edge);
  }
  // Same (source, target, activity) can be produced by both walks: keep one.
  const unique = new Map(edges.map((e) => [e.id, e]));
  return { currentId, nodes: [...nodes.values()], edges: [...unique.values()], truncated: { incoming: incoming.truncated, outgoing: outgoing.truncated } };
}

interface WalkResult {
  nodes: Map<string, ContextNode>;
  edges: ContextEdge[];
  truncated: boolean;
  count: number;
}

function walkOutgoing(graph: CallGraph, currentId: string, limits: SubgraphLimits): WalkResult {
  const nodes = new Map<string, ContextNode>();
  const edges: ContextEdge[] = [];
  const visited = new Set<string>([currentId]);
  const depthOf = new Map<string, number>([[currentId, 0]]);
  const queue: string[] = [currentId];
  let truncated = false;
  let count = 0;

  while (queue.length > 0) {
    const sourceId = queue.shift()!;
    const source = graph.nodes.get(sourceId);
    if (!source) continue;
    const depth = depthOf.get(sourceId) ?? 0;
    let branchTruncated = false;
    for (const call of effectiveEdges(source)) {
      const edgeId = `out:${sourceId}:${call.activityId}->${call.targetId ?? "?"}`;
      if (!call.targetId) {
        const id = `unresolved:${sourceId}:${call.activityId}`;
        if (!nodes.has(id)) {
          nodes.set(id, { id, kind: "unresolved", name: `${call.callType} (${call.expressionType ?? "expression"})`, side: "outgoing", depth: depth + 1 });
          count++;
        }
        edges.push({ id: edgeId, source: sourceId, target: id, callType: call.callType, activityId: call.activityId, back: false, side: "outgoing" });
        continue;
      }
      const targetId = call.targetId;
      if (visited.has(targetId)) {
        const back = targetId === currentId || (depthOf.get(targetId) ?? 0) <= depth;
        edges.push({ id: edgeId, source: sourceId, target: targetId, callType: call.callType, activityId: call.activityId, back, side: "outgoing" });
        continue;
      }
      if (depth + 1 > limits.maxDepth || count >= limits.maxNodes) {
        branchTruncated = true;
        continue;
      }
      visited.add(targetId);
      depthOf.set(targetId, depth + 1);
      const target = graph.nodes.get(targetId);
      const kind: ContextNodeKind = !target ? (graph.status === "loading" ? "pending" : "missing") : target.status === "missing" ? "missing" : "workflow";
      nodes.set(targetId, { id: targetId, kind, definitionId: targetId, name: target?.name ?? targetId, side: "outgoing", depth: depth + 1, version: target?.version, isPublished: target?.isPublished });
      count++;
      edges.push({ id: edgeId, source: sourceId, target: targetId, callType: call.callType, activityId: call.activityId, back: false, side: "outgoing" });
      if (kind === "workflow") queue.push(targetId);
    }
    if (branchTruncated) {
      truncated = true;
      const id = `truncated:out:${sourceId}`;
      nodes.set(id, { id, kind: "truncated", name: "More callees not shown", side: "outgoing", depth: depth + 1 });
      edges.push({ id: `${id}:edge`, source: sourceId, target: id, callType: "Dispatch", activityId: "", back: false, side: "outgoing" });
    }
  }
  return { nodes, edges, truncated, count };
}

function walkIncoming(graph: CallGraph, currentId: string, limits: SubgraphLimits, alreadyCounted: number): WalkResult {
  // Reverse index: callee -> calls. Expression targets cannot be matched here: a call whose target is
  // computed at runtime never appears as an incoming edge of any workflow.
  const callers = new Map<string, { callerId: string; callType: CallType; activityId: string }[]>();
  for (const node of graph.nodes.values()) {
    for (const call of effectiveEdges(node)) {
      if (!call.targetId) continue;
      const list = callers.get(call.targetId) ?? [];
      list.push({ callerId: node.definitionId, callType: call.callType, activityId: call.activityId });
      callers.set(call.targetId, list);
    }
  }

  const nodes = new Map<string, ContextNode>();
  const edges: ContextEdge[] = [];
  const visited = new Set<string>([currentId]);
  const depthOf = new Map<string, number>([[currentId, 0]]);
  const queue: string[] = [currentId];
  let truncated = false;
  let count = alreadyCounted;

  while (queue.length > 0) {
    const targetId = queue.shift()!;
    const depth = depthOf.get(targetId) ?? 0;
    let branchTruncated = false;
    for (const call of callers.get(targetId) ?? []) {
      const edgeId = `in:${call.callerId}:${call.activityId}->${targetId}`;
      if (visited.has(call.callerId)) {
        const back = call.callerId === currentId || (depthOf.get(call.callerId) ?? 0) <= depth;
        edges.push({ id: edgeId, source: call.callerId, target: targetId, callType: call.callType, activityId: call.activityId, back, side: "incoming" });
        continue;
      }
      if (depth + 1 > limits.maxDepth || count >= limits.maxNodes) {
        branchTruncated = true;
        continue;
      }
      visited.add(call.callerId);
      depthOf.set(call.callerId, depth + 1);
      const caller = graph.nodes.get(call.callerId);
      nodes.set(call.callerId, { id: call.callerId, kind: "workflow", definitionId: call.callerId, name: caller?.name ?? call.callerId, side: "incoming", depth: depth + 1, version: caller?.version, isPublished: caller?.isPublished });
      count++;
      edges.push({ id: edgeId, source: call.callerId, target: targetId, callType: call.callType, activityId: call.activityId, back: false, side: "incoming" });
      queue.push(call.callerId);
    }
    if (branchTruncated) {
      truncated = true;
      const id = `truncated:in:${targetId}`;
      nodes.set(id, { id, kind: "truncated", name: "More callers not shown", side: "incoming", depth: depth + 1 });
      edges.push({ id: `${id}:edge`, source: id, target: targetId, callType: "Dispatch", activityId: "", back: false, side: "incoming" });
    }
  }
  return { nodes, edges, truncated, count };
}
