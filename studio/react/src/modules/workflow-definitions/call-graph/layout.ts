import type { ContextEdge, ContextNode, ContextSide } from "./subgraph";

export interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export const CONTEXT_NODE_WIDTH = 200;
export const CONTEXT_NODE_HEIGHT = 60;
const GAP_X = 120;
const GAP_Y = 24;

/**
 * Layered layout for the external nodes around the grouping box: callers on the left, callees on the
 * right, one column per distance with the roots/leaves furthest away. Within a column nodes are ordered
 * by the average vertical position of their neighbours in the previous column (one barycenter pass),
 * which keeps most edges short and uncrossed, and the column is centred on the box.
 */
export function layoutContextNodes(nodes: ContextNode[], edges: ContextEdge[], box: Rect): Map<string, { x: number; y: number }> {
  const positions = new Map<string, { x: number; y: number }>();
  const boxCenterY = box.y + box.height / 2;
  for (const side of ["incoming", "outgoing"] as ContextSide[]) {
    const sideNodes = nodes.filter((n) => n.side === side);
    const maxDepth = sideNodes.reduce((m, n) => Math.max(m, n.depth), 0);
    let previous: ContextNode[] = [];
    for (let depth = 1; depth <= maxDepth; depth++) {
      const layer = sideNodes.filter((n) => n.depth === depth);
      const prevIndex = new Map(previous.map((n, i) => [n.id, i]));
      const neighbourRank = (n: ContextNode) => {
        const ranks = edges
          .filter((e) => (side === "outgoing" ? e.target === n.id && prevIndex.has(e.source) : e.source === n.id && prevIndex.has(e.target)))
          .map((e) => prevIndex.get(side === "outgoing" ? e.source : e.target)!);
        return ranks.length ? ranks.reduce((a, b) => a + b, 0) / ranks.length : Number.MAX_SAFE_INTEGER;
      };
      layer.sort((a, b) => neighbourRank(a) - neighbourRank(b) || a.name.localeCompare(b.name));
      const totalHeight = layer.length * CONTEXT_NODE_HEIGHT + (layer.length - 1) * GAP_Y;
      const x = side === "incoming" ? box.x - depth * (CONTEXT_NODE_WIDTH + GAP_X) : box.x + box.width + GAP_X + (depth - 1) * (CONTEXT_NODE_WIDTH + GAP_X);
      layer.forEach((n, i) => positions.set(n.id, { x: Math.round(x), y: Math.round(boxCenterY - totalHeight / 2 + i * (CONTEXT_NODE_HEIGHT + GAP_Y)) }));
      previous = layer;
    }
  }
  return positions;
}
