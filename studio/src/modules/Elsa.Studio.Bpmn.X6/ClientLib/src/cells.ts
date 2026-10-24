/**
 * What one shown plane draws, as X6 cells. Shapes and edges take their geometry from the document's DI; what kind of
 * marker an element shows comes from its semantic element, read through the kernel's document.
 *
 * A cell's id is the id of the element it draws, so the DOM, overlays and markers all name elements as the contract
 * does. A name the DI gives label bounds is drawn as its own cell there.
 */
import type { Edge, Node } from '@antv/x6';
import type { ModdleElement } from '../../../Elsa.Studio.Bpmn/ClientLib/src/index.js';
import type { BpmnDocument, BpmnEdgeView, BpmnShapeView, Bounds, Point } from '../../../Elsa.Studio.Bpmn/ClientLib/src/modeling/index.js';
import { ACTIVITY_MARKER_GLYPHS, EVENT_DEFINITION_GLYPHS, GATEWAY_GLYPHS, type Glyph, TASK_TYPE_GLYPHS } from './glyphs.js';
import type { ShownPlane } from './planes.js';
import { MUTED, SHAPES, STROKE, SURFACE } from './shapes.js';

const LABEL_SUFFIX = '::label';

/** X6's attributes for a cell, by selector, and for one selector. */
type CellAttrs = NonNullable<Node.Metadata['attrs']>;
type Attrs = CellAttrs[string];

/** The id of the cell that draws the element's label. */
function labelCellId(elementId: string): string {
    return `${elementId}${LABEL_SUFFIX}`;
}

const EVENTS = new Set(['bpmn:StartEvent', 'bpmn:EndEvent', 'bpmn:IntermediateCatchEvent', 'bpmn:IntermediateThrowEvent', 'bpmn:BoundaryEvent']);
const THROWING_EVENTS = new Set(['bpmn:EndEvent', 'bpmn:IntermediateThrowEvent']);
const GATEWAYS = new Set(['bpmn:ExclusiveGateway', 'bpmn:ParallelGateway', 'bpmn:InclusiveGateway', 'bpmn:EventBasedGateway', 'bpmn:ComplexGateway']);
const ACTIVITIES = new Set([
    'bpmn:Task', 'bpmn:UserTask', 'bpmn:ServiceTask', 'bpmn:ScriptTask', 'bpmn:SendTask', 'bpmn:ReceiveTask', 'bpmn:ManualTask',
    'bpmn:BusinessRuleTask', 'bpmn:CallActivity', 'bpmn:SubProcess', 'bpmn:Transaction', 'bpmn:AdHocSubProcess'
]);
const SUBPROCESSES = new Set(['bpmn:SubProcess', 'bpmn:Transaction', 'bpmn:AdHocSubProcess']);
const CONTAINERS = new Set(['bpmn:Participant', 'bpmn:Lane']);
const DATA_OBJECTS = new Set(['bpmn:DataObjectReference', 'bpmn:DataObject', 'bpmn:DataInput', 'bpmn:DataOutput']);

// Paths drawn in a 36x50 data object box and a 50x50 data store box, scaled to the shape's bounds.
const DATA_OBJECT_PATH = 'M 0 0 L 26 0 L 36 10 L 36 50 L 0 50 Z M 26 0 L 26 10 L 36 10';
const DATA_STORE_PATH = 'M 0 8 C 0 -2.7 50 -2.7 50 8 L 50 42 C 50 52.7 0 52.7 0 42 Z M 0 8 C 0 18.7 50 18.7 50 8 M 0 13 C 0 23.7 50 23.7 50 13';

/** Layers, back to front: containers by depth, then shapes, edges, labels and badges. */
const Z = { container: 0, shape: 100, edge: 200, label: 300, badge: 400 };

export interface PlaneCells {
    nodes: Node.Metadata[];
    edges: Edge.Metadata[];
    /** The bounds each drawn element covers, by element id, for placing badges. */
    bounds: Map<string, Bounds>;
    /** The type of each drawn element, by element id. */
    types: Map<string, string>;
    /** The point halfway along each drawn edge, by element id, where its badges go. */
    midpoints: Map<string, Point>;
}

/**
 * The cells for `plane`. `drillable` says whether a collapsed subprocess has a plane to open, so it shows the button
 * that opens it.
 */
export function planeCells(document: BpmnDocument, plane: ShownPlane, drillable: (subprocessId: string) => boolean): PlaneCells {
    const nodes: Node.Metadata[] = [];
    const edges: Edge.Metadata[] = [];
    const bounds = new Map<string, Bounds>();
    const types = new Map<string, string>();
    const midpoints = new Map<string, Point>();
    const depths = depthsOf(plane.shapes);

    for (const shape of plane.shapes) {
        // An element drawn twice on one plane keeps its first drawing: a cell id must be unique, and it is the element id.
        if (bounds.has(shape.elementId)) continue;
        const element = document.element(shape.elementId);
        bounds.set(shape.elementId, shape.bounds);
        types.set(shape.elementId, shape.type);
        nodes.push(shapeCell(shape, element, depths.get(shape) ?? 0, drillable));
        const label = externalLabel(shape, shape.bounds);
        if (label) nodes.push(label);
    }

    for (const edge of plane.edges) {
        if (bounds.has(edge.elementId) || edge.waypoints.length < 2) continue;
        bounds.set(edge.elementId, boundsOfPoints(edge.waypoints));
        types.set(edge.elementId, edge.type);
        midpoints.set(edge.elementId, midpointOf(edge.waypoints));
        edges.push(edgeCell(edge, document.element(edge.elementId)));
        if (edge.name && edge.label) nodes.push(labelCell(edge.elementId, edge.name, edge.label));
    }

    return { nodes, edges, bounds, types, midpoints };
}

function shapeCell(shape: BpmnShapeView, element: ModdleElement | undefined, depth: number, drillable: (subprocessId: string) => boolean): Node.Metadata {
    const { x, y, width, height } = shape.bounds;
    const base = { id: shape.elementId, x, y, width, height, zIndex: Z.shape };
    const name = shape.name ?? '';

    if (EVENTS.has(shape.type)) return { ...base, shape: SHAPES.event, attrs: eventAttrs(shape, element) };
    if (GATEWAYS.has(shape.type)) return { ...base, shape: SHAPES.gateway, attrs: { marker: glyphAttrs(gatewayGlyph(shape), false) } };
    if (ACTIVITIES.has(shape.type)) {
        const container = shape.isExpanded === true;
        return { ...base, zIndex: container ? Z.container + 10 + depth : Z.shape, shape: SHAPES.activity, attrs: activityAttrs(shape, element, name, drillable) };
    }
    if (CONTAINERS.has(shape.type)) {
        return {
            ...base,
            zIndex: Z.container + depth,
            shape: SHAPES.container,
            // Read bottom to top, centred in the header band.
            attrs: { label: { text: name, transform: `rotate(-90 15 ${height / 2})`, refX: null, refY: null, x: 15, y: height / 2 } }
        };
    }
    if (DATA_OBJECTS.has(shape.type)) return { ...base, shape: SHAPES.data, attrs: { body: { refDResetOffset: DATA_OBJECT_PATH } } };
    if (shape.type === 'bpmn:DataStoreReference') return { ...base, shape: SHAPES.data, attrs: { body: { refDResetOffset: DATA_STORE_PATH } } };
    if (shape.type === 'bpmn:TextAnnotation') {
        // The bracket keeps its 10-pixel arms whatever the annotation's width, so it is drawn at the shape's own height.
        return { ...base, shape: SHAPES.annotation, attrs: { body: { d: `M 10 0 L 0 0 L 0 ${height} L 10 ${height}` }, label: { text: String(element?.text ?? '') } } };
    }
    if (shape.type === 'bpmn:Group') return { ...base, zIndex: Z.container, shape: SHAPES.group, attrs: { label: { text: groupLabel(element) } } };
    return { ...base, shape: SHAPES.unknown, attrs: { label: { text: name || shape.type.replace(/^bpmn:/, '') } } };
}

function eventAttrs(shape: BpmnShapeView, element: ModdleElement | undefined): CellAttrs {
    const throwing = THROWING_EVENTS.has(shape.type);
    const nonInterrupting = (shape.type === 'bpmn:BoundaryEvent' && element?.cancelActivity === false)
        || (shape.type === 'bpmn:StartEvent' && element?.isInterrupting === false);
    const intermediate = shape.type === 'bpmn:IntermediateCatchEvent' || shape.type === 'bpmn:IntermediateThrowEvent' || shape.type === 'bpmn:BoundaryEvent';
    const dash = nonInterrupting ? '4 3' : null;
    return {
        body: { strokeWidth: shape.type === 'bpmn:EndEvent' ? 3.5 : 1.5, strokeDasharray: dash },
        ring: { display: intermediate ? 'block' : 'none', strokeDasharray: dash },
        icon: glyphAttrs(eventGlyph(element), throwing)
    };
}

function activityAttrs(shape: BpmnShapeView, element: ModdleElement | undefined, name: string, drillable: (subprocessId: string) => boolean): CellAttrs {
    const subprocess = SUBPROCESSES.has(shape.type);
    const container = shape.isExpanded === true;
    const collapsedSubprocess = subprocess && !container;
    const loop = element?.loopCharacteristics as ModdleElement | undefined;
    const loopGlyph = !loop ? null
        : loop.$type === 'bpmn:MultiInstanceLoopCharacteristics'
            ? (loop.isSequential ? ACTIVITY_MARKER_GLYPHS.multiInstanceSequential : ACTIVITY_MARKER_GLYPHS.multiInstanceParallel)
            : ACTIVITY_MARKER_GLYPHS.loop;
    // A collapsed call activity shows the collapsed marker too, as bpmn-js draws it.
    const collapsed = collapsedSubprocess || (shape.type === 'bpmn:CallActivity' && shape.isExpanded === false);
    // In the order BPMN lists them along the bottom edge.
    const markers = [
        loopGlyph,
        element?.isForCompensation === true ? ACTIVITY_MARKER_GLYPHS.compensation : null,
        collapsed ? ACTIVITY_MARKER_GLYPHS.collapsed : null,
        shape.type === 'bpmn:AdHocSubProcess' ? ACTIVITY_MARKER_GLYPHS.adHoc : null
    ].filter((glyph): glyph is Glyph => glyph !== null);

    return {
        body: {
            strokeWidth: shape.type === 'bpmn:CallActivity' ? 4 : 1.5,
            strokeDasharray: element?.triggeredByEvent === true ? '2 3' : null,
            fill: container ? 'none' : SURFACE
        },
        innerBorder: { display: shape.type === 'bpmn:Transaction' ? 'block' : 'none' },
        typeMarker: glyphAttrs(TASK_TYPE_GLYPHS[localName(shape.type)] ?? null, false),
        // An expanded subprocess names itself at its top left, out of the way of what it contains.
        label: container
            ? { text: name, refX: 10, refY: 8, textAnchor: 'start', textVerticalAnchor: 'top', textWrap: { width: -20, height: 20, ellipsis: true } }
            : { text: name },
        markerA: markerAttrs(markers, 0),
        markerB: markerAttrs(markers, 1),
        markerC: markerAttrs(markers, 2),
        markerD: markerAttrs(markers, 3),
        drill: { display: collapsedSubprocess && drillable(shape.elementId) ? 'block' : 'none' }
    };
}

/** The bottom markers, centred as a row of 16-pixel glyphs. */
function markerAttrs(markers: Glyph[], index: number): Attrs {
    const glyph = markers[index];
    if (!glyph) return { display: 'none' };
    return { ...glyphAttrs(glyph, false), refX2: (index - (markers.length - 1) / 2) * 18 };
}

function glyphAttrs(glyph: Glyph | null, throwing: boolean): Attrs {
    if (!glyph) return { display: 'none' };
    const solid = glyph.fill === 'solid' || (glyph.fill === 'direction' && throwing);
    return { display: 'block', d: glyph.d, fill: solid ? STROKE : 'none', strokeWidth: glyph.strokeWidth ?? 1.2 };
}

function eventGlyph(element: ModdleElement | undefined): Glyph | null {
    const definitions = (element?.eventDefinitions as ModdleElement[] | undefined) ?? [];
    if (definitions.length === 0) return null;
    if (definitions.length > 1) return element?.parallelMultiple === true ? EVENT_DEFINITION_GLYPHS.parallelmultiple : EVENT_DEFINITION_GLYPHS.multiple;
    const kind = definitions[0].$type.replace(/^bpmn:/, '').replace(/EventDefinition$/, '').toLowerCase();
    return EVENT_DEFINITION_GLYPHS[kind] ?? null;
}

function gatewayGlyph(shape: BpmnShapeView): Glyph | null {
    return GATEWAY_GLYPHS[localName(shape.type)] ?? null;
}

/** `bpmn:UserTask` as the glyph tables key it: `userTask`. */
function localName(type: string): string {
    return type.replace(/^bpmn:/, '').replace(/^./, first => first.toLowerCase());
}

function groupLabel(element: ModdleElement | undefined): string {
    const category = element?.categoryValueRef as ModdleElement | undefined;
    return typeof category?.value === 'string' ? category.value : '';
}

/** The label of an event, gateway or data shape, drawn under the shape or where its DI places it. */
function externalLabel(shape: BpmnShapeView, bounds: Bounds): Node.Metadata | undefined {
    const external = EVENTS.has(shape.type) || GATEWAYS.has(shape.type) || DATA_OBJECTS.has(shape.type) || shape.type === 'bpmn:DataStoreReference';
    if (!external || !shape.name) return undefined;
    const label = shape.label ?? { x: bounds.x + bounds.width / 2 - 50, y: bounds.y + bounds.height + 5, width: 100, height: 28 };
    return labelCell(shape.elementId, shape.name, label);
}

function labelCell(elementId: string, name: string, bounds: Bounds): Node.Metadata {
    return { id: labelCellId(elementId), shape: SHAPES.label, ...bounds, zIndex: Z.label, attrs: { label: { text: name } } };
}

function edgeCell(edge: BpmnEdgeView, element: ModdleElement | undefined): Edge.Metadata {
    const points = edge.waypoints;
    const line: Attrs = { stroke: STROKE, strokeDasharray: null, sourceMarker: null, targetMarker: null };
    switch (edge.type) {
        case 'bpmn:SequenceFlow':
            line.targetMarker = { name: 'block', width: 10, height: 8 };
            if (isDefaultFlow(edge, element)) line.sourceMarker = { name: 'path', d: 'M 6 -6 L 14 6', fill: 'none', strokeWidth: 1.5 };
            break;
        case 'bpmn:MessageFlow':
            line.strokeDasharray = '8 5';
            line.sourceMarker = { name: 'circle', r: 4, fill: SURFACE };
            line.targetMarker = { name: 'block', width: 10, height: 8, open: true, fill: SURFACE };
            break;
        case 'bpmn:DataInputAssociation':
        case 'bpmn:DataOutputAssociation':
            line.strokeDasharray = '2 4';
            line.targetMarker = { name: 'block', width: 8, height: 8, open: true, fill: 'none' };
            break;
        default:
            // Associations and anything else: dotted, with an arrowhead only where the association says so.
            line.stroke = MUTED;
            line.strokeDasharray = '2 4';
            if (element?.associationDirection === 'One' || element?.associationDirection === 'Both') line.targetMarker = { name: 'block', width: 8, height: 8, open: true, fill: 'none' };
            if (element?.associationDirection === 'Both') line.sourceMarker = { name: 'block', width: 8, height: 8, open: true, fill: 'none' };
    }

    return {
        id: edge.elementId,
        shape: SHAPES.edge,
        source: points[0],
        target: points[points.length - 1],
        vertices: points.slice(1, -1),
        zIndex: Z.edge,
        attrs: { line }
    };
}

function isDefaultFlow(edge: BpmnEdgeView, element: ModdleElement | undefined): boolean {
    const source = element?.sourceRef as ModdleElement | undefined;
    return !!source && (source.default as ModdleElement | undefined)?.id === edge.elementId;
}

/** How deeply each shape is nested in containers on its plane, so an inner container draws above an outer one. */
function depthsOf(shapes: readonly BpmnShapeView[]): Map<BpmnShapeView, number> {
    const byElementId = new Map(shapes.map(shape => [shape.elementId, shape]));
    const depths = new Map<BpmnShapeView, number>();
    for (const shape of shapes) {
        let depth = 0;
        const seen = new Set<string>();
        for (let parent = shape.parentId ? byElementId.get(shape.parentId) : undefined; parent && !seen.has(parent.elementId); parent = parent.parentId ? byElementId.get(parent.parentId) : undefined) {
            seen.add(parent.elementId);
            depth++;
        }
        depths.set(shape, depth);
    }
    return depths;
}

/** The point halfway along a polyline. */
function midpointOf(points: readonly Point[]): Point {
    const lengths = points.slice(1).map((point, index) => Math.hypot(point.x - points[index].x, point.y - points[index].y));
    let remaining = lengths.reduce((sum, length) => sum + length, 0) / 2;
    for (let index = 0; index < lengths.length; index++) {
        if (remaining <= lengths[index] && lengths[index] > 0) {
            const ratio = remaining / lengths[index];
            return { x: points[index].x + (points[index + 1].x - points[index].x) * ratio, y: points[index].y + (points[index + 1].y - points[index].y) * ratio };
        }
        remaining -= lengths[index];
    }
    return points[0];
}

export function boundsOfPoints(points: readonly Point[]): Bounds {
    const xs = points.map(point => point.x);
    const ys = points.map(point => point.y);
    const x = Math.min(...xs);
    const y = Math.min(...ys);
    return { x, y, width: Math.max(...xs) - x, height: Math.max(...ys) - y };
}
