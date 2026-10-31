import {Edge, Graph, Node} from '@antv/x6';
import {GraphBinding} from './graph-bindings';

export type SequenceLayoutOrientation = 'vertical' | 'horizontal';

const SequenceNodeGap = 64;
const SequenceMinimumStride = 160;

export function normalizeSequenceOrientation(value?: string | null): SequenceLayoutOrientation {
    return value === 'horizontal' ? 'horizontal' : 'vertical';
}

export function isHorizontalSequence(binding: GraphBinding): boolean {
    return normalizeSequenceOrientation(binding.layoutOrientation) === 'horizontal';
}

export function withSuppressedGraphUpdated<T>(binding: GraphBinding, action: () => T): T {
    binding.suppressGraphUpdated = (binding.suppressGraphUpdated || 0) + 1;
    try {
        return action();
    } finally {
        binding.suppressGraphUpdated = Math.max(0, (binding.suppressGraphUpdated || 0) - 1);
    }
}

export function arrangeSequenceGraph(binding: GraphBinding, orderedNodes?: Node<Node.Properties>[]) {
    const graph = binding.graph;
    const nodes = orderedNodes ?? sortSequenceNodes(graph.getNodes(), binding);
    const horizontal = isHorizontalSequence(binding);
    const nodeSizes = nodes.map(node => node.getSize());
    const crossAxisSize = Math.max(0, ...nodeSizes.map(size => horizontal ? size.height : size.width));

    withSuppressedGraphUpdated(binding, () => {
        graph.batchUpdate('sequence-layout', () => {
            const mutationOptions = {sequenceLayout: true};
            let offset = 0;

            nodes.forEach((node, index) => {
                const size = nodeSizes[index];
                node.setPosition(
                    horizontal
                        ? {x: offset, y: (crossAxisSize - size.height) / 2}
                        : {x: (crossAxisSize - size.width) / 2, y: offset},
                    mutationOptions,
                );

                const nodeExtent = horizontal ? size.width : size.height;
                offset += Math.max(SequenceMinimumStride, nodeExtent + SequenceNodeGap);
            });

            graph.getEdges().forEach(edge => graph.removeCell(edge, mutationOptions));
            buildSequenceEdges(graph, nodes, horizontal).forEach(edge => graph.addEdge(edge, mutationOptions));
        });
    });
}

export function setSequenceOrientation(binding: GraphBinding, orientation: string) {
    binding.layoutOrientation = normalizeSequenceOrientation(orientation);
    arrangeSequenceGraph(binding);
}

export function moveSelectedSequenceNode(binding: GraphBinding, direction: number): boolean {
    const delta = direction < 0 ? -1 : 1;
    const ordered = sortSequenceNodes(binding.graph.getNodes(), binding);
    const selectedIndex = ordered.findIndex(node => binding.graph.isSelected(node));
    if (selectedIndex < 0) return false;

    const targetIndex = selectedIndex + delta;
    if (targetIndex < 0 || targetIndex >= ordered.length) return false;

    const next = [...ordered];
    [next[selectedIndex], next[targetIndex]] = [next[targetIndex], next[selectedIndex]];
    arrangeSequenceGraph(binding, next);
    binding.graph.cleanSelection();
    binding.graph.select(next[targetIndex]);
    return true;
}

export function sortSequenceNodes(nodes: Node<Node.Properties>[], binding: GraphBinding): Node<Node.Properties>[] {
    const horizontal = isHorizontalSequence(binding);
    return [...nodes].sort((a, b) => {
        const aPosition = a.getPosition();
        const bPosition = b.getPosition();
        const primary = horizontal ? aPosition.x - bPosition.x : aPosition.y - bPosition.y;
        if (primary !== 0) return primary;
        return horizontal ? aPosition.y - bPosition.y : aPosition.x - bPosition.x;
    });
}

function buildSequenceEdges(graph: Graph, nodes: Node<Node.Properties>[], horizontal: boolean): Edge<Edge.Properties>[] {
    return nodes.slice(0, -1).map((node, index) => {
        const next = nodes[index + 1];
        return graph.createEdge({
            id: `${node.id}:sequence:${next.id}`,
            shape: 'elsa-sequence-edge',
            source: {
                cell: node.id,
                anchor: {name: horizontal ? 'right' : 'bottom'},
            },
            target: {
                cell: next.id,
                anchor: {name: horizontal ? 'left' : 'top'},
            },
            zIndex: -1,
        });
    });
}
