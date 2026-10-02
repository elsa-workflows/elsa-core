import type { Node, Edge } from '@xyflow/react';
import type { ElsaPort } from '../types';

export interface HappyPathLayoutOptions {
    /** Horizontal gap (px) between adjacent columns. Same for every gap. */
    rankSep?: number;
    /** Vertical gap (px) between adjacent rows. Same for every gap. */
    rowSep?: number;
    /** Fallback dimensions for nodes that haven't been measured yet. */
    defaultWidth?: number;
    defaultHeight?: number;
}

const DEFAULTS: Required<HappyPathLayoutOptions> = {
    rankSep: 130,
    rowSep: 100,
    defaultWidth: 240,
    defaultHeight: 80,
};

function nodeWidth(n: Node, fallback: number): number {
    const w = n.style?.width ?? n.measured?.width;
    return typeof w === 'number' ? w : fallback;
}

function nodeHeight(n: Node, fallback: number): number {
    const h = n.style?.height ?? n.measured?.height;
    return typeof h === 'number' ? h : fallback;
}

// Outgoing edges are sorted by the index of their source handle in the source
// node's declared out-port list. So for an If activity declared as
// [True, False, …], the edge wired to "True" is the happy-path continuation
// regardless of which one the user wired up first. Edges whose source handle
// doesn't match any declared port (or have no handle) fall to the end.
// Stable sort preserves insertion order for ties — important when an activity
// has a synthesized default port and several edges share the same handle id.

/**
 * Arranges workflow nodes left-to-right with the happy path on row 0 and
 * branches stacked below.
 *
 * Algorithm:
 *  1. Compute each node's column via topological longest-path depth from
 *     start nodes (no incoming edges). Cycles/orphans pin to col 0.
 *  2. Walk the "happy chain" from each start: follow the default outgoing
 *     edge (Done → empty → alphabetical) on the same row. Side branches
 *     get fresh rows beneath.
 *  3. Compute each column's width as the max node width in that column;
 *     each row's height as the max node height in that row. Apply a fixed
 *     `rankSep` between columns and `rowSep` between rows so all gaps are
 *     equal regardless of node sizes.
 *  4. Center each node inside its column×row cell so smoothstep edges
 *     between same-row nodes route as straight lines.
 */
export function arrangeHappyPath<TData extends Record<string, unknown>>(
    nodes: Node<TData>[],
    edges: Edge[],
    opts: HappyPathLayoutOptions = {},
): Node<TData>[] {
    if (nodes.length === 0) return nodes;

    const { rankSep, rowSep, defaultWidth, defaultHeight } = { ...DEFAULTS, ...opts };

    // ---- Adjacency ------------------------------------------------------
    const outs = new Map<string, Edge[]>();
    const ins = new Map<string, Edge[]>();
    for (const n of nodes) { outs.set(n.id, []); ins.set(n.id, []); }
    for (const e of edges) {
        outs.get(e.source)?.push(e);
        ins.get(e.target)?.push(e);
    }

    // Build per-node port-index lookup: portIndex.get(nodeId).get(handleId) → 0-based
    // position in the activity's declared out-port list. This is the source of
    // truth for "which outcome is the happy path".
    const portIndexByNode = new Map<string, Map<string, number>>();
    for (const n of nodes) {
        const data = n.data as { ports?: ElsaPort[] } | undefined;
        const outPorts = (data?.ports ?? []).filter(p => p.group === 'out');
        const map = new Map<string, number>();
        outPorts.forEach((p, i) => { if (p.id) map.set(p.id, i); });
        portIndexByNode.set(n.id, map);
    }

    // Sort each source's outgoing edges by their handle's port index so the
    // first declared out port wins the happy path, regardless of wire-up order.
    for (const [sourceId, arr] of outs) {
        const idxByHandle = portIndexByNode.get(sourceId);
        arr.sort((a, b) => {
            const ai = idxByHandle?.get(a.sourceHandle ?? '') ?? Number.MAX_SAFE_INTEGER;
            const bi = idxByHandle?.get(b.sourceHandle ?? '') ?? Number.MAX_SAFE_INTEGER;
            return ai - bi;
        });
    }

    // ---- Columns: topological longest-path (Kahn's) --------------------
    const depth = new Map<string, number>();
    const inDeg = new Map<string, number>();
    for (const n of nodes) inDeg.set(n.id, ins.get(n.id)!.length);
    const ready: string[] = [];
    for (const n of nodes) {
        if (inDeg.get(n.id) === 0) {
            depth.set(n.id, 0);
            ready.push(n.id);
        }
    }
    while (ready.length) {
        const id = ready.shift()!;
        const d = depth.get(id) ?? 0;
        for (const e of outs.get(id) ?? []) {
            const t = e.target;
            const td = depth.get(t);
            if (td === undefined || d + 1 > td) depth.set(t, d + 1);
            const nd = (inDeg.get(t) ?? 0) - 1;
            inDeg.set(t, nd);
            if (nd === 0) ready.push(t);
        }
    }
    // Anything not depth-resolved (cycles, fully orphaned with self-cycles) → col 0.
    for (const n of nodes) if (!depth.has(n.id)) depth.set(n.id, 0);

    // ---- Rows: walk the happy chain, branches below --------------------
    const row = new Map<string, number>();
    let nextRow = 0;

    const visit = (id: string, currentRow: number): void => {
        if (row.has(id)) return;
        row.set(id, currentRow);
        const myOuts = outs.get(id) ?? [];
        for (let i = 0; i < myOuts.length; i++) {
            const target = myOuts[i].target;
            if (row.has(target)) continue;
            if (i === 0) {
                // Default branch: continue on the same row.
                visit(target, currentRow);
            } else {
                // Side branch: claim a brand-new row below.
                nextRow++;
                visit(target, nextRow);
            }
        }
    };

    // Prefer triggers (canStartWorkflow=true) as the very first start so the
    // workflow's "real" entry point ends up on the top row when possible.
    const starts = nodes
        .filter(n => (ins.get(n.id)?.length ?? 0) === 0)
        .sort((a, b) => {
            const aStart = (a.data as { activity?: { canStartWorkflow?: boolean } } | undefined)?.activity?.canStartWorkflow === true ? 0 : 1;
            const bStart = (b.data as { activity?: { canStartWorkflow?: boolean } } | undefined)?.activity?.canStartWorkflow === true ? 0 : 1;
            return aStart - bStart;
        })
        .map(n => n.id);
    if (starts.length === 0) starts.push(nodes[0].id);

    for (const startId of starts) {
        if (!row.has(startId)) {
            visit(startId, nextRow);
            nextRow++;
        }
    }
    // Reach any remaining disconnected nodes.
    for (const n of nodes) {
        if (!row.has(n.id)) {
            visit(n.id, nextRow);
            nextRow++;
        }
    }

    // ---- Column widths + X anchors -------------------------------------
    const numCols = Math.max(...depth.values()) + 1;
    const colWidth = new Array<number>(numCols).fill(0);
    for (const n of nodes) {
        const c = depth.get(n.id) ?? 0;
        const w = nodeWidth(n, defaultWidth);
        if (w > colWidth[c]) colWidth[c] = w;
    }
    const colX = new Array<number>(numCols);
    let cursorX = 0;
    for (let c = 0; c < numCols; c++) {
        if (c > 0) cursorX += rankSep;
        colX[c] = cursorX;
        cursorX += colWidth[c];
    }

    // ---- Row heights + Y anchors ---------------------------------------
    const numRows = Math.max(...row.values()) + 1;
    const rowHeight = new Array<number>(numRows).fill(0);
    for (const n of nodes) {
        const r = row.get(n.id) ?? 0;
        const h = nodeHeight(n, defaultHeight);
        if (h > rowHeight[r]) rowHeight[r] = h;
    }
    const rowY = new Array<number>(numRows);
    let cursorY = 0;
    for (let r = 0; r < numRows; r++) {
        if (r > 0) cursorY += rowSep;
        rowY[r] = cursorY;
        cursorY += rowHeight[r];
    }

    // ---- Apply: centre each node within its (column, row) cell ---------
    return nodes.map(n => {
        const c = depth.get(n.id) ?? 0;
        const r = row.get(n.id) ?? 0;
        const w = nodeWidth(n, defaultWidth);
        const h = nodeHeight(n, defaultHeight);
        const x = colX[c] + (colWidth[c] - w) / 2;
        const y = rowY[r] + (rowHeight[r] - h) / 2;
        return { ...n, position: { x, y } };
    });
}
