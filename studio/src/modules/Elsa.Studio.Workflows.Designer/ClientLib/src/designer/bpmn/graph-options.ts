/**
 * The X6 graph configuration the BPMN canvas is created with.
 *
 * A separate, pure function on purpose. "Read-only" is the property this whole adapter rests on --
 * W14, not W9c, is what makes a BPMN diagram editable -- and a property that only exists inside a
 * `new Graph(...)` call can only be checked by reading the code. Here it can be asserted:
 * `__tests__/graph-options.test.ts` walks every interaction flag and fails if one of them is
 * anything other than `false`.
 */
import type { CellView, Options as GraphOptions } from '@antv/x6';

/**
 * Every interaction X6 knows how to offer, each one refused.
 *
 * Spelled out key by key rather than written `interacting: false` so that an X6 upgrade which adds
 * a new interaction is a visible gap in this list and a failing test, rather than a new way to edit
 * a diagram that nothing in Studio asked for.
 */
export const BPMN_READ_ONLY_INTERACTING: Record<CellView.InteractionNames, boolean> = {
    nodeMovable: false,
    edgeMovable: false,
    edgeLabelMovable: false,
    arrowheadMovable: false,
    vertexMovable: false,
    vertexAddable: false,
    vertexDeletable: false,
    useEdgeTools: false,
    magnetConnectable: false,
    stopDelegateOnDragging: false,
    toolsAddable: false,
};

export interface BpmnGraphSettings {
    /** Whether the canvas may be panned by dragging it. Defaults to true. */
    readonly panning?: boolean;
    /** Whether the mouse wheel zooms. Defaults to true. */
    readonly mousewheel?: boolean;
    /** Whether the background grid is drawn. Defaults to true. */
    readonly grid?: boolean;
}

/**
 * Builds the options for a read-only BPMN graph.
 *
 * Beyond the refused interactions, two things are deliberately absent and stay absent:
 *
 *  * **no connecting.** `validateConnection` and `validateMagnet` both refuse unconditionally, so
 *    even a caller that later enables a magnet cannot draw an edge by accident.
 *  * **no embedding.** BPMN containment -- a subprocess's body, a lane's membership -- is a fact of
 *    the document, and X6's embedding would make it a fact of the canvas that dragging could
 *    change. Boundary events and lane contents are positioned, never parented.
 */
export function createBpmnGraphOptions(settings?: BpmnGraphSettings): Partial<GraphOptions.Manual> {
    return {
        autoResize: true,
        grid: settings?.grid === false
            ? false
            : { visible: true, size: 10, type: 'dot', args: { color: 'var(--elsa-bpmn-grid)', thickness: 1 } },
        panning: { enabled: settings?.panning !== false },
        mousewheel: {
            enabled: settings?.mousewheel !== false,
            factor: 1.05,
            minScale: 0.2,
            maxScale: 4,
        },
        interacting: { ...BPMN_READ_ONLY_INTERACTING },
        connecting: {
            allowBlank: false,
            allowLoop: false,
            allowNode: false,
            allowEdge: false,
            allowPort: false,
            allowMulti: false,
            validateMagnet: () => false,
            validateConnection: () => false,
        },
        embedding: { enabled: false },
        preventDefaultBlankAction: false,
    };
}
