import {Node, Edge} from "@antv/x6";
import {graphBindings} from "./graph-bindings";
import {arrangeSequenceGraph, withSuppressedGraphUpdated} from "./sequence-mode";

export async function pasteCells(graphId: string, nodeProps: Node.Properties[], edgeProps: Edge.Properties[]) {
    // Get graph reference.
    const binding = graphBindings[graphId];
    const {graph} = binding;

    // Create nodes and edges from node props and edge props.
    const nodes = nodeProps.map(x => graph.createNode(x));
    const edges = edgeProps.map(x => graph.createEdge(x));

    // Add the nodes and edges to the graph.
    if (binding.mode === 'sequence') {
        withSuppressedGraphUpdated(binding, () => graph.addCell(nodes));
        arrangeSequenceGraph(binding);
        await binding.interop.raiseGraphUpdated();
    } else {
        graph.addCell(nodes);
        graph.addCell(edges);
    }

    // Wait for the new cells to be rendered.
    requestAnimationFrame(() => {
        graph.cleanSelection();
        graph.select(binding.mode === 'sequence' ? nodes : [...nodes, ...edges]);
    });
}
