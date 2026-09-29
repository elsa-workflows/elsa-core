import {calculateActivitySize, getActivityMeasurementScopeClass} from "./calculate-activity-size";
import {Activity, Size} from "../models";
import {graphBindings} from "./graph-bindings";
import {Node} from "@antv/x6";
import {arrangeSequenceGraph} from "./sequence-mode";

export async function updateActivitySize(elementId: string, activityModel: Activity | string, size?: Size, portCount?: number) {
    // Get wrapper element.
    const wrapper = document.getElementById(elementId);

    if (wrapper == null) {
        console.warn(`Could not find wrapper element with ID ${elementId}`);
        return;
    }

    // Get container element.
    const container = wrapper.closest('.graph-container');
    const measurementScopeClass = getActivityMeasurementScopeClass(wrapper);

    // Get graph ID.
    const graphId = container.id;

    // Get graph reference.
    const graphBinding = graphBindings[graphId];
    const graph = graphBinding.graph;

    // Parse activity model.
    const activity = typeof activityModel === 'string' ? JSON.parse(activityModel) : activityModel;

    // Calculate the size of the activity.
    const rect = await calculateActivitySize(activity, portCount, measurementScopeClass);
    let width = rect.width;
    let height = rect.height;

    // Get the node from the graph and update its size.
    const activityId = activity.id;
    const node = graph.getNodes().find(x => x.id == activityId);

    if (node == null) {
        console.warn(`Could not find node with ID ${activityId} in graph ${graphId}`);
        return;
    }
    
    if (!!size) {
        if (size.width > width)
            width = size.width;

        if (size.height > height)
            height = size.height;
    }

    // Only update the node size if it actually changed, to avoid triggering unnecessary graph update events.
    const currentSize = node.size();
    if (Math.abs(currentSize.width - width) > 0.5 || Math.abs(currentSize.height - height) > 0.5) {
        // Suppress graph updated events for programmatic size adjustments (e.g., initial render sizing).
        // This prevents unwanted auto-saves when the calculated size differs from the stored size,
        // which commonly occurs with NotFoundActivity where the server resets sizes on save.
        graphBinding.suppressGraphUpdated = (graphBinding.suppressGraphUpdated || 0) + 1;
        try {
            node.size(width, height);
            if (graphBinding.mode === 'sequence')
                arrangeSequenceGraph(graphBinding);
        } finally {
            graphBinding.suppressGraphUpdated = Math.max(0, (graphBinding.suppressGraphUpdated || 0) - 1);
        }
    }
}

/**
 * Enforces the minimum size on a node based on its activity content.
 * If the current size is smaller than the minimum, the node will snap back to the minimum size.
 * @param node The X6 node to enforce minimum size on
 * @returns true if the size was adjusted, false otherwise
 */
export async function enforceMinimumNodeSize(node: Node, measurementScopeClass = ''): Promise<boolean> {
    const activity: Activity = node.data;
    
    if (!activity) {
        return false;
    }

    // Determine the number of ports on the node, defaulting to 0 if unavailable.
    const portCount = node.ports?.items?.length ?? 0;

    // Calculate the minimum size based on content and port count
    const minSize = await calculateActivitySize(activity, portCount, measurementScopeClass);
    const currentSize = node.size();
    
    let needsResize = false;
    let newWidth = currentSize.width;
    let newHeight = currentSize.height;
    
    if (currentSize.width < minSize.width) {
        newWidth = minSize.width;
        needsResize = true;
    }
    
    if (currentSize.height < minSize.height) {
        newHeight = minSize.height;
        needsResize = true;
    }
    
    if (needsResize) {
        node.size(newWidth, newHeight);
    }
    
    return needsResize;
}
