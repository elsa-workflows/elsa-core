import {graphBindings} from "./graph-bindings";

export async function selectActivity(elementId: string, activityId: string) {
    // Get wrapper element.
    const wrapper = document.getElementById(elementId);

    if (wrapper == null) {
        console.warn(`Could not find wrapper element with ID ${elementId}`);
        return;
    }

    // Get container element.
    const container = wrapper.closest('.graph-container');

    // Get graph ID.
    const graphId = container.id;

    // Get graph reference.
    const {graph} = graphBindings[graphId];
    
    // Get activity node.
    const node = graph.getNodes().find(x => x.id == activityId);

    if (node == null) {
        console.warn(`Could not find node with ID ${activityId} in graph ${graphId}`);
        return;
    }

    // Select the node.
    graph.select(node);
    
    // Center the selected node.
    graph.centerCell(node);
}