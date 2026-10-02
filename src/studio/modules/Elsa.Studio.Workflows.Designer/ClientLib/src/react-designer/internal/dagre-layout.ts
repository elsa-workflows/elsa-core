import * as dagre from '@dagrejs/dagre';
import type { Node, Edge } from '@xyflow/react';

export interface LayoutOptions {
    rankdir?: 'LR' | 'TB' | 'RL' | 'BT';
    ranksep?: number;
    nodesep?: number;
}

// Mirrors the X6 designer's DagreLayout settings (rankdir LR, ranksep 35,
// nodesep 15) so users get a comparable visual result when toggling engines.
const defaults: Required<LayoutOptions> = {
    rankdir: 'LR',
    ranksep: 80,
    nodesep: 30,
};

export function dagreLayout<TData extends Record<string, unknown>>(
    nodes: Node<TData>[],
    edges: Edge[],
    opts: LayoutOptions = {},
): Node<TData>[] {
    if (nodes.length === 0) return nodes;

    const options = { ...defaults, ...opts };
    const g = new dagre.graphlib.Graph();
    g.setGraph({
        rankdir: options.rankdir,
        ranksep: options.ranksep,
        nodesep: options.nodesep,
        marginx: 20,
        marginy: 20,
    });
    g.setDefaultEdgeLabel(() => ({}));

    for (const node of nodes) {
        const width = (node.style as any)?.width ?? node.measured?.width ?? 200;
        const height = (node.style as any)?.height ?? node.measured?.height ?? 80;
        g.setNode(node.id, { width, height });
    }

    for (const edge of edges) {
        if (edge.source && edge.target) g.setEdge(edge.source, edge.target);
    }

    dagre.layout(g);

    return nodes.map(node => {
        const laid = g.node(node.id);
        if (!laid) return node;
        // dagre returns the center; React Flow positions are top-left.
        const width = (node.style as any)?.width ?? node.measured?.width ?? 200;
        const height = (node.style as any)?.height ?? node.measured?.height ?? 80;
        return {
            ...node,
            position: { x: laid.x - width / 2, y: laid.y - height / 2 },
        };
    });
}
