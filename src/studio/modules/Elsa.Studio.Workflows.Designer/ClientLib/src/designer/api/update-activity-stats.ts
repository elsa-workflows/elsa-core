import {ActivityStats} from "../models";
import {graphBindings} from "./graph-bindings";

export async function updateActivityStats(elementId: string, activityId: string, activityStats: ActivityStats) {
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
    
    // Get the node from the graph and update its size.
    const node = graph.getNodes().find(x => x.id == activityId);

    if (node == null) {
        console.warn(`Could not find node with ID ${activityId} in graph ${graphId}`);
        return;
    }
    
    node.setProp('activityStats', activityStats);
}