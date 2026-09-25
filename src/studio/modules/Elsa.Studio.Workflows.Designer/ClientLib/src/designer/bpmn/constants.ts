/**
 * Names and layering constants shared by the pure cell builder and the X6 mount layer.
 *
 * Deliberately free of imports so that `cells.ts` -- the layer the fixture tests gate -- can be
 * loaded without pulling `@antv/x6` into the test environment.
 */

/** The CSS class `createBpmnGraph` puts on the container so the BPMN custom properties resolve. */
export const BPMN_DESIGNER_CLASS = 'elsa-bpmn-diagram-designer';

// Node shapes, one per BPMN element family. Tasks, call activities and subprocesses share one
// rectangle shape and differ by attrs: their geometry, markers and labels are the same structure.
export const BPMN_EVENT_SHAPE = 'bpmn-event';
export const BPMN_ACTIVITY_SHAPE = 'bpmn-activity';
export const BPMN_GATEWAY_SHAPE = 'bpmn-gateway';
export const BPMN_POOL_SHAPE = 'bpmn-pool';
export const BPMN_LANE_SHAPE = 'bpmn-lane';

// Edge shapes.
export const BPMN_FLOW_SHAPE = 'bpmn-flow';
export const BPMN_ASSOCIATION_SHAPE = 'bpmn-association';

/**
 * The node anchor that pins an edge end to the exact BPMN DI waypoint.
 *
 * See `shapes.ts` for why the built-in `topLeft` anchor cannot be used for this.
 */
export const BPMN_WAYPOINT_ANCHOR = 'bpmnWaypoint';

/**
 * Cell id prefixes for the things that are not flow elements.
 *
 * A BPMN element's node keeps its element id verbatim, because `selectBpmnElement`,
 * `updateBpmnElementStats` and the instance overlay are all keyed by it and a translation table
 * between the two is one more thing that can drift. Lanes, pools and compensation associations get
 * a prefix instead: lane and pool ids share the document's id space with elements, and an
 * association has no id of its own at all.
 */
export const BPMN_POOL_CELL_PREFIX = 'bpmn-pool:';
export const BPMN_LANE_CELL_PREFIX = 'bpmn-lane:';
export const BPMN_ASSOCIATION_CELL_PREFIX = 'bpmn-association:';

/**
 * Painting order.
 *
 * X6 paints by ascending `zIndex`, so the bands below encode what BPMN needs to be legible:
 * containers behind their contents, sequence flows behind the shapes they join, boundary events in
 * front of the host they sit on, and a nested scope in front of the subprocess that contains it.
 * Everything is derived from the scope depth the view model reports, so an arbitrarily deep nesting
 * keeps the same relative order without a special case.
 */
export const POOL_Z_INDEX = 0;
export const LANE_Z_INDEX = 1;

/** Elements of a scope at `depth`; the root scope is depth 0. */
export function elementZIndex(depth: number): number {
    return 10 * (depth + 1);
}

/** A boundary event draws in front of the host it is attached to, which is in its own scope. */
export function boundaryZIndex(depth: number): number {
    return elementZIndex(depth) + 5;
}

/** A scope's sequence flows draw behind that scope's elements but in front of its container. */
export function flowZIndex(depth: number): number {
    return elementZIndex(depth) - 5;
}
