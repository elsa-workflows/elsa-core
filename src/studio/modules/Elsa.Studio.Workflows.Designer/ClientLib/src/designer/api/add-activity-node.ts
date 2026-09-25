import {Node} from "@antv/x6";
import {graphBindings} from "./graph-bindings";
import {arrangeSequenceGraph, withSuppressedGraphUpdated} from "./sequence-mode";

export async function addActivityNode(graphId: string, node: Node.Properties) {
    // Get graph reference.
    const binding = graphBindings[graphId];
    const {graph} = binding;

    // Convert the node coordinates from page to local.
    const {x, y} = graph.pageToLocal(node.position);

    node.position = {x, y};
    node.size = {width: 200, height: 50};
    node.id = node.id!;

    // Add the node to the graph.
    if (binding.mode === 'sequence') {
        withSuppressedGraphUpdated(binding, () => graph.addNode(node));
        arrangeSequenceGraph(binding);
        await binding.interop.raiseGraphUpdated();
    } else {
        graph.addNode(node);
    }

    graph.cleanSelection();
    graph.select(node.id);
}
