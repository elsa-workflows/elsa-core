import {Edge, Graph} from '@antv/x6';

/** The subset of the graph the hover tools listen to. */
export type EdgeHoverEvents = Pick<Graph, 'on'>;

const vertices = 'vertices';
const removeButton = 'button-remove';

/**
 * Shows the vertex handles and the remove button on the edge under the pointer.
 *
 * X6 appends a tool on every `addTools` call, and it does not raise `edge:mouseleave` when the pointer
 * leaves an edge across one of its tools or straight onto another cell. So each tool is added only if
 * the edge lacks it, and the remove button of the last hovered edge is cleared as soon as the pointer
 * is seen over another edge, a node, the blank canvas, or outside the graph.
 */
export function registerEdgeHoverTools(graph: EdgeHoverEvents, enabled: boolean) {
    if (!enabled)
        return;

    let hovered: Edge | null = null;

    const hideRemoveButton = () => {
        if (hovered?.hasTool(removeButton))
            hovered.removeTool(removeButton);
        hovered = null;
    };

    graph.on('edge:mouseenter', ({edge}) => {
        if (hovered !== edge)
            hideRemoveButton();

        if (!edge.hasTool(vertices))
            edge.addTools({name: vertices});
        if (!edge.hasTool(removeButton))
            edge.addTools({name: removeButton, args: {distance: 20}});

        hovered = edge;
    });

    graph.on('edge:mouseleave', hideRemoveButton);
    graph.on('node:mouseenter', hideRemoveButton);
    // `blank:mousemove` fires only while a button is held; hovering empty canvas raises `blank:mouseover`.
    graph.on('blank:mouseover', hideRemoveButton);
    graph.on('graph:mouseleave', hideRemoveButton);
    graph.on('edge:removed', ({edge}) => {
        if (hovered === edge)
            hovered = null;
    });
}
