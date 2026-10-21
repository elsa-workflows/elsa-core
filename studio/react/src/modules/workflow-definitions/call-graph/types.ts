import type { CallType } from "./config";

/** A call found in a definition's activity tree. `targetId` is undefined when the target is an expression. */
export interface CallEdge {
  callerId: string;
  /** Undefined when the target input is an expression and cannot be resolved statically. */
  targetId: string | undefined;
  callType: CallType;
  /** The activity that makes the call, and its type. */
  activityId: string;
  activityType: string;
  /** Set when `targetId` is undefined: the expression syntax that computes the target. */
  expressionType?: string;
}

export type CallGraphNodeStatus = "loaded" | "missing";

export interface CallGraphNode {
  definitionId: string;
  name: string;
  /** Version id and number of the inspected version; absent for missing nodes. */
  versionId?: string;
  version?: number;
  isPublished?: boolean;
  status: CallGraphNodeStatus;
  /** Outgoing edges of the stored (inspected) version. */
  storedEdges: CallEdge[];
  /** Outgoing edges of the editor's unsaved state, when the definition is open in the designer. */
  liveEdges?: CallEdge[];
}

export type CallGraphBuildStatus = "idle" | "loading" | "ready" | "error" | "cancelled";

export interface CallGraphWarning {
  code: "max-requests" | "max-nodes" | "timeout" | "load-failed";
  message: string;
}

/** The session-wide call graph: one node per workflow definition, edges kept on the caller. */
export interface CallGraph {
  nodes: Map<string, CallGraphNode>;
  status: CallGraphBuildStatus;
  warnings: CallGraphWarning[];
  error?: unknown;
  /** True once every definition the list returned has been inspected. */
  complete: boolean;
}

/** Effective outgoing edges of a node: unsaved editor state first, stored version otherwise. */
export function effectiveEdges(node: CallGraphNode): CallEdge[] {
  return node.liveEdges ?? node.storedEdges;
}
