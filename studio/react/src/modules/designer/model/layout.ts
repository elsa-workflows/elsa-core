import dagre from "@dagrejs/dagre";
import type { Flowchart } from "@/lib/api/types/activities";
import { DEFAULT_NODE_HEIGHT, DEFAULT_NODE_WIDTH, activitySize } from "./display";

export interface LayoutNode {
  id: string;
  width: number;
  height: number;
}

export interface LayoutEdge {
  source: string;
  target: string;
  /** Index of the source port the edge leaves from and how many ports the source has (for ordering and straightening). */
  portIndex?: number;
  portCount?: number;
}

/**
 * Left-to-right dagre layout. Nodes are centred within their column (dagre's default), which is
 * what keeps a chain of single-port steps on one straight line now that ports sit at the card's
 * vertical middle. Returns top-left positions keyed by node id.
 */
export interface LayoutResult {
  positions: Record<string, { x: number; y: number }>;
  /** Waypoints (flow coordinates) for edges that dagre routed around other nodes, keyed by `source->target`. */
  vertices: Record<string, { x: number; y: number }[]>;
}

export function edgeKey(e: LayoutEdge) {
  return `${e.source}->${e.target}`;
}

export function layoutGraph(nodes: LayoutNode[], edges: LayoutEdge[]): LayoutResult {
  const g = new dagre.graphlib.Graph();
  g.setGraph({ rankdir: "LR", ranksep: 90, nodesep: 40, marginx: 20, marginy: 20 });
  g.setDefaultEdgeLabel(() => ({}));
  for (const n of nodes) g.setNode(n.id, { width: n.width, height: n.height });
  for (const e of edges) if (g.hasNode(e.source) && g.hasNode(e.target) && e.source !== e.target) g.setEdge(e.source, e.target);
  dagre.layout(g);
  const positions: Record<string, { x: number; y: number }> = {};
  for (const n of nodes) {
    const l = g.node(n.id);
    if (l) positions[n.id] = { x: Math.round(l.x - l.width / 2), y: l.y - l.height / 2 };
  }
  orderBranchesByPort(nodes, edges, positions);
  straightenChains(nodes, edges, positions);
  resolveColumnOverlaps(nodes, positions);

  // Edges that skip at least one column get dagre's intermediate points so they route around the
  // cards in between. Edges between neighbouring columns stay plain (dagre's single mid-point would
  // only add a kink after the straightening pass).
  const sizeOf = new Map(nodes.map((n) => [n.id, n]));
  const columnX = [...new Set(nodes.map((n) => Math.round(positions[n.id].x + (sizeOf.get(n.id)?.width ?? 0) / 2)))].sort((a, b) => a - b);
  const columnIndex = (id: string) => columnX.indexOf(Math.round(positions[id].x + (sizeOf.get(id)?.width ?? 0) / 2));
  const vertices: Record<string, { x: number; y: number }[]> = {};
  for (const e of edges) {
    if (e.source === e.target || !g.hasEdge(e.source, e.target) || !positions[e.source] || !positions[e.target]) continue;
    if (Math.abs(columnIndex(e.target) - columnIndex(e.source)) < 2) continue;
    const points = (g.edge(e.source, e.target) as { points?: { x: number; y: number }[] } | undefined)?.points ?? [];
    const inner = points.slice(1, -1).map((p) => ({ x: Math.round(p.x), y: Math.round(p.y) }));
    if (inner.length > 0) vertices[edgeKey(e)] = inner;
  }
  return { positions, vertices };
}

/** Pushes cards apart (top to bottom) wherever two in the same column would overlap. */
function resolveColumnOverlaps(nodes: LayoutNode[], positions: Record<string, { x: number; y: number }>) {
  const centerX = (n: LayoutNode) => Math.round(positions[n.id].x + n.width / 2);
  const columns = new Map<number, LayoutNode[]>();
  for (const n of nodes) if (positions[n.id]) columns.set(centerX(n), [...(columns.get(centerX(n)) ?? []), n]);
  for (const column of columns.values()) {
    column.sort((a, b) => positions[a.id].y - positions[b.id].y);
    for (let i = 1; i < column.length; i++) {
      const prev = column[i - 1];
      const cur = column[i];
      const minY = positions[prev.id].y + prev.height + GAP;
      if (positions[cur.id].y < minY) positions[cur.id] = { ...positions[cur.id], y: minY };
    }
  }
}

const GAP = 24;

/**
 * dagre orders siblings to minimise crossings but knows nothing about ports, so the "False" branch
 * can end up above "True". Within each column, targets of the same source are re-assigned the
 * column's vertical slots in port order.
 */
function orderBranchesByPort(nodes: LayoutNode[], edges: LayoutEdge[], positions: Record<string, { x: number; y: number }>) {
  const bySource = new Map<string, LayoutEdge[]>();
  for (const e of edges) if (e.portIndex !== undefined) bySource.set(e.source, [...(bySource.get(e.source) ?? []), e]);
  const size = new Map(nodes.map((n) => [n.id, n]));
  const columnOf = (id: string) => Math.round(positions[id].x + (size.get(id)?.width ?? 0) / 2);
  for (const list of bySource.values()) {
    // Group this source's targets by column (dagre aligns the centres of a rank, not the left edges).
    const byColumn = new Map<number, LayoutEdge[]>();
    for (const e of list) {
      if (!positions[e.target]) continue;
      const col = columnOf(e.target);
      byColumn.set(col, [...(byColumn.get(col) ?? []), e]);
    }
    for (const group of byColumn.values()) {
      if (group.length < 2) continue;
      const slots = group.map((e) => positions[e.target].y).sort((a, b) => a - b);
      const ordered = [...group].sort((a, b) => (a.portIndex ?? 0) - (b.portIndex ?? 0));
      // Assign slots top-down, re-packing so differently sized cards don't overlap.
      let y = slots[0];
      ordered.forEach((e, i) => {
        if (i > 0) y = Math.max(y, slots[i]);
        positions[e.target] = { ...positions[e.target], y };
        y += (size.get(e.target)?.height ?? 60) + GAP;
      });
    }
  }
}

/**
 * Puts every step on the horizontal centre line of the step(s) it follows, column by column, so a
 * path reads as one straight spine through the middle of each card regardless of card height or of
 * which port the connection leaves from. When one source feeds several steps in the same column the
 * first one (in port order) stays on the spine and the others stack below it; a step fed by several
 * sources sits at the middle of their centre lines. The overlap pass afterwards separates clashes.
 */
function straightenChains(nodes: LayoutNode[], edges: LayoutEdge[], positions: Record<string, { x: number; y: number }>) {
  const size = new Map(nodes.map((n) => [n.id, n]));
  const incoming = new Map<string, LayoutEdge[]>();
  for (const e of edges) if (e.source !== e.target && positions[e.source] && positions[e.target]) incoming.set(e.target, [...(incoming.get(e.target) ?? []), e]);
  const centerX = (n: LayoutNode) => (positions[n.id] ? Math.round(positions[n.id].x + n.width / 2) : undefined);
  const centerY = (id: string) => positions[id].y + (size.get(id)?.height ?? 0) / 2;
  const columns = [...new Set(nodes.map(centerX).filter((x): x is number => x !== undefined))].sort((a, b) => a - b);
  for (const cx of columns) {
    const column = nodes.filter((n) => centerX(n) === cx).sort((a, b) => positions[a.id].y - positions[b.id].y);
    const columnIds = new Set(column.map((n) => n.id));
    // Targets already placed for each source in this column, so later siblings stack below the first.
    const placedFor = new Map<string, LayoutNode[]>();
    for (const n of column) {
      const ins = incoming.get(n.id) ?? [];
      if (ins.length === 0) continue;
      const sources = [...new Set(ins.map((e) => e.source))];
      let y: number;
      if (sources.length === 1) {
        const source = sources[0];
        const siblings = edges
          .filter((e) => e.source === source && e.target !== e.source && columnIds.has(e.target))
          .sort((a, b) => (a.portIndex ?? 0) - (b.portIndex ?? 0));
        const first = siblings[0]?.target;
        const placed = placedFor.get(source) ?? [];
        if (first === n.id || placed.length === 0) {
          y = centerY(source) - n.height / 2;
        } else {
          const prev = placed[placed.length - 1];
          y = positions[prev.id].y + prev.height + GAP;
        }
        placedFor.set(source, [...placed, n]);
      } else {
        const mid = sources.reduce((sum, id) => sum + centerY(id), 0) / sources.length;
        y = mid - n.height / 2;
      }
      positions[n.id] = { x: positions[n.id].x, y };
    }
  }
}

/** Auto layout for a flowchart; measured sizes win over stored ones. `outPortsOf` supplies port order per activity. */
export function autoLayout(
  container: Flowchart,
  measured?: Map<string, { width: number; height: number }>,
  outPortsOf?: (activity: Flowchart["activities"][number]) => { name: string }[],
): LayoutResult {
  const byId = new Map(container.activities.map((a) => [a.id, a]));
  return layoutGraph(
    container.activities.map((a) => {
      const size = measured?.get(a.id) ?? activitySize(a) ?? { width: DEFAULT_NODE_WIDTH, height: DEFAULT_NODE_HEIGHT };
      return { id: a.id, width: size.width, height: size.height };
    }),
    container.connections.map((c) => {
      const source = byId.get(c.source.activity);
      const ports = source && outPortsOf ? outPortsOf(source) : undefined;
      const portIndex = ports ? Math.max(0, ports.findIndex((p) => p.name === c.source.port)) : undefined;
      return { source: c.source.activity, target: c.target.activity, portIndex, portCount: ports?.length };
    }),
  );
}
