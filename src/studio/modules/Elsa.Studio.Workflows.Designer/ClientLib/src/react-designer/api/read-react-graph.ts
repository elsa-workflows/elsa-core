import { reactBindings } from '../bindings';

interface ReadGraphCells {
    cells: Array<Record<string, any>>;
    layoutOrientation?: string;
}

// Returns a payload shaped like X6's read-graph result so the C# side can
// reuse the same X6Graph deserialization path: each cell carries a `shape`
// discriminator ('elsa-activity' | 'elsa-edge') plus the underlying fields.
export function readReactGraph(graphId: string): ReadGraphCells {
    const binding = reactBindings[graphId];
    if (!binding) return { cells: [] };

    const graph = binding.readGraph();
    const cells: Array<Record<string, any>> = [];

    for (const node of graph.nodes ?? []) {
        cells.push({
            shape: 'elsa-activity',
            id: node.id,
            position: node.position,
            size: node.size,
            data: node.data,
            ports: node.ports,
            activityStats: node.activityStats,
        });
    }

    for (const edge of graph.edges ?? []) {
        // Skip dangling edges (matches X6's read-graph filter).
        if (!edge.source?.cell || !edge.target?.cell) continue;
        cells.push({
            shape: 'elsa-edge',
            source: edge.source,
            target: edge.target,
            vertices: edge.vertices ?? [],
        });
    }

    return { cells, layoutOrientation: graph.layoutOrientation };
}
