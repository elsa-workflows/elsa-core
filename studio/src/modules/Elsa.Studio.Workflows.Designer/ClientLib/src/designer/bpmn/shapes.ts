/**
 * The X6 shape, edge and anchor registrations the BPMN cells refer to by name.
 *
 * Registration is structure only: every shape declares its markup and the defaults that make an
 * unset attribute harmless, and `cells.ts` supplies everything that varies per element. Splitting
 * it that way is what lets the mapping be tested without a browser -- nothing here needs to run for
 * `buildBpmnX6Cells` to produce the right cells.
 *
 * All colours are CSS custom properties resolved by `css/designer.bpmn.css`; see `palette.ts`.
 */
import { Graph, Point } from '@antv/x6';
import {
    BPMN_ACTIVITY_SHAPE,
    BPMN_ASSOCIATION_SHAPE,
    BPMN_EVENT_SHAPE,
    BPMN_FLOW_SHAPE,
    BPMN_GATEWAY_SHAPE,
    BPMN_LANE_SHAPE,
    BPMN_POOL_SHAPE,
    BPMN_WAYPOINT_ANCHOR,
} from './constants';
import { CONTAINER_STROKE, CONTAINER_SURFACE, EDGE, HEADER_SURFACE, MUTED, STROKE, SURFACE, TEXT } from './palette';

/**
 * The badge circle and its number, shared by every element shape, grouped so the badge carries one
 * `aria-label` and one `<title>` tooltip rather than leaving its state -- faulted, cancelled,
 * blocked -- to be read off its colour alone.
 */
const STATS_MARKUP = [
    {
        tagName: 'g',
        selector: 'statsBadgeGroup',
        children: [
            { tagName: 'title', selector: 'statsBadgeTitle' },
            { tagName: 'circle', selector: 'statsBadge' },
            { tagName: 'text', selector: 'statsLabel' },
        ],
    },
];

const STATS_ATTRS = {
    statsBadgeGroup: {
        'aria-label': null,
    },
    statsBadgeTitle: {
        text: '',
    },
    statsBadge: {
        display: 'none',
        r: 9,
        cx: 0,
        cy: 0,
        refX: '100%',
        refY: 0,
        pointerEvents: 'none',
    },
    statsLabel: {
        display: 'none',
        text: '',
        fontSize: 10,
        fontWeight: 600,
        textAnchor: 'middle',
        textVerticalAnchor: 'middle',
        refX: '100%',
        refY: 0,
        pointerEvents: 'none',
    },
};

/**
 * Defaults for a text element.
 *
 * Every positional and typographic attribute is spelled out rather than left to the shape's
 * ancestry. X6's built-in shapes carry an `attrs.text` entry that matches *every* `<text>` element
 * in the markup by CSS selector, so a value left unset here would silently inherit the built-in
 * label's centre-of-the-node placement.
 */
const textDefaults = (extra: Record<string, unknown> = {}) => ({
    display: 'none',
    text: '',
    fill: TEXT,
    fontSize: 12,
    fontWeight: 400,
    fontFamily: 'inherit',
    textAnchor: 'middle',
    textVerticalAnchor: 'middle',
    refX: '50%',
    refY: '50%',
    pointerEvents: 'none',
    ...extra,
});

/** Defaults for a glyph path: hidden, and painted from the centre of whatever placed it. */
const glyphDefaults = {
    display: 'none',
    d: '',
    fill: 'none',
    stroke: STROKE,
    strokeWidth: 1.2,
    strokeLinejoin: 'round',
    strokeLinecap: 'round',
    pointerEvents: 'none',
};

/**
 * Registers everything the BPMN adapter draws with. Idempotent: every registration passes X6's
 * `force` flag, so calling it twice replaces rather than throws.
 */
export function registerBpmnShapes(): void {
    registerWaypointAnchor();

    // -- events ---------------------------------------------------------------------------------
    // One shape for all five event types. What distinguishes them -- the second ring, the thick end
    // stroke, the dashed non-interrupting boundary, the event-definition icon -- is attributes, so
    // the same node can be re-attributed without being replaced.
    Graph.registerNode(
        BPMN_EVENT_SHAPE,
        {
            inherit: 'circle',
            markup: [
                { tagName: 'circle', selector: 'body' },
                { tagName: 'circle', selector: 'ring' },
                { tagName: 'path', selector: 'icon' },
                { tagName: 'text', selector: 'label' },
                ...STATS_MARKUP,
            ],
            attrs: {
                body: {
                    refCx: '50%',
                    refCy: '50%',
                    refR: '50%',
                    fill: SURFACE,
                    stroke: STROKE,
                    strokeWidth: 1.5,
                },
                ring: { display: 'none', fill: 'none', stroke: STROKE, strokeWidth: 1.5, pointerEvents: 'none' },
                icon: glyphDefaults,
                label: textDefaults({ fontSize: 11, refY: '100%', refY2: 6, textVerticalAnchor: 'top' }),
                ...STATS_ATTRS,
            },
        },
        true);

    // -- gateways -------------------------------------------------------------------------------
    Graph.registerNode(
        BPMN_GATEWAY_SHAPE,
        {
            inherit: 'polygon',
            markup: [
                { tagName: 'polygon', selector: 'body' },
                { tagName: 'path', selector: 'marker' },
                { tagName: 'text', selector: 'label' },
                ...STATS_MARKUP,
            ],
            attrs: {
                body: {
                    refPoints: '0,0.5 0.5,0 1,0.5 0.5,1',
                    fill: SURFACE,
                    stroke: STROKE,
                    strokeWidth: 1.5,
                },
                marker: glyphDefaults,
                label: textDefaults({ fontSize: 11, refY: '100%', refY2: 6, textVerticalAnchor: 'top' }),
                ...STATS_ATTRS,
            },
        },
        true);

    // -- tasks, subprocesses and call activities ------------------------------------------------
    // These share one rectangle. A task, a collapsed subprocess, an expanded subprocess container
    // and a call activity differ in border, markers and where the name sits -- all attributes --
    // and never in structure, so a separate shape per element type would be three copies of this.
    Graph.registerNode(
        BPMN_ACTIVITY_SHAPE,
        {
            inherit: 'rect',
            markup: [
                { tagName: 'rect', selector: 'body' },
                { tagName: 'rect', selector: 'innerBorder' },
                { tagName: 'path', selector: 'typeMarker' },
                { tagName: 'text', selector: 'label' },
                { tagName: 'text', selector: 'subLabel' },
                { tagName: 'path', selector: 'markerA' },
                { tagName: 'path', selector: 'markerB' },
                { tagName: 'path', selector: 'markerC' },
                ...STATS_MARKUP,
            ],
            attrs: {
                body: {
                    refWidth: '100%',
                    refHeight: '100%',
                    rx: 10,
                    ry: 10,
                    fill: SURFACE,
                    stroke: STROKE,
                    strokeWidth: 1.5,
                },
                // BPMN's double border for a transaction subprocess.
                innerBorder: {
                    display: 'none',
                    x: 4,
                    y: 4,
                    refWidth: -8,
                    refHeight: -8,
                    rx: 7,
                    ry: 7,
                    fill: 'none',
                    stroke: STROKE,
                    strokeWidth: 1.5,
                    pointerEvents: 'none',
                },
                typeMarker: glyphDefaults,
                label: textDefaults(),
                subLabel: textDefaults({ fontSize: 10, fill: MUTED }),
                markerA: glyphDefaults,
                markerB: glyphDefaults,
                markerC: glyphDefaults,
                ...STATS_ATTRS,
            },
        },
        true);

    // -- pools and lanes ------------------------------------------------------------------------
    // Containers are drawn, never structural: `pointerEvents: 'none'` keeps them out of hit-testing
    // entirely, so a click inside a lane reaches the element under the pointer or, where there is
    // none, the canvas itself. That is the whole of "pools and lanes contain elements visually
    // only" as far as X6 is concerned -- there is no embedding to undo, and nothing that could
    // reparent an element by dragging it.
    for (const shape of [BPMN_POOL_SHAPE, BPMN_LANE_SHAPE]) {
        Graph.registerNode(
            shape,
            {
                inherit: 'rect',
                markup: [
                    { tagName: 'rect', selector: 'body' },
                    { tagName: 'rect', selector: 'header' },
                    { tagName: 'text', selector: 'label' },
                ],
                attrs: {
                    body: {
                        refWidth: '100%',
                        refHeight: '100%',
                        fill: CONTAINER_SURFACE,
                        stroke: CONTAINER_STROKE,
                        strokeWidth: 1.5,
                        pointerEvents: 'none',
                    },
                    header: { fill: HEADER_SURFACE, stroke: CONTAINER_STROKE, strokeWidth: 1.5, pointerEvents: 'none' },
                    label: textDefaults({ display: 'block', fontWeight: 600 }),
                },
            },
            true);
    }

    // -- sequence flows -------------------------------------------------------------------------
    // `normal` router and connector: the vertices are the document's own waypoints, so anything
    // that re-routes them would be drawing a line the author did not.
    Graph.registerEdge(
        BPMN_FLOW_SHAPE,
        {
            inherit: 'edge',
            attrs: {
                line: {
                    stroke: EDGE,
                    strokeWidth: 1.5,
                    targetMarker: { name: 'block', width: 10, height: 8 },
                },
            },
            router: { name: 'normal' },
            connector: { name: 'normal' },
        },
        true);

    // -- compensation associations --------------------------------------------------------------
    // Dashed with an open arrowhead, and routed by X6: the payload records an association as a
    // property of the boundary event rather than as an element, so there is no id for the document's
    // BPMNEdge to be matched by and no waypoints to draw it from.
    Graph.registerEdge(
        BPMN_ASSOCIATION_SHAPE,
        {
            inherit: 'edge',
            attrs: {
                line: {
                    stroke: EDGE,
                    strokeWidth: 1.5,
                    strokeDasharray: '4 4',
                    targetMarker: { name: 'block', width: 8, height: 8, open: true },
                },
            },
            router: { name: 'normal' },
            connector: { name: 'normal' },
        },
        true);
}

/**
 * Pins an edge end to an exact point, expressed relative to the node's own rectangle.
 *
 * X6's built-in `topLeft` anchor cannot do this job: it measures the node's *rendered* bounding
 * box, and BPMN nodes draw their name outside the shape, so the box it would measure is the shape
 * plus its label and the offset would land somewhere the document never mentioned. Reading
 * `cell.getBBox()` instead uses the geometry the view model gave the node and nothing else, so the
 * anchor is the document's waypoint to the pixel and stays correct whether or not the node has been
 * rendered yet.
 */
function registerWaypointAnchor(): void {
    Graph.registerAnchor(
        BPMN_WAYPOINT_ANCHOR,
        function bpmnWaypoint(view, _magnet, _ref, args) {
            const bbox = view.cell.getBBox();
            const dx = typeof args?.dx === 'number' ? args.dx : 0;
            const dy = typeof args?.dy === 'number' ? args.dy : 0;

            return new Point(bbox.x + dx, bbox.y + dy);
        },
        true);
}
