/**
 * Resizing a shape on a plane, with what follows its border: the boundary events attached to it and the ends of the
 * edges that connect it. A resize changes layout (DI) only.
 */
import type { ModdleElement } from '../binding.js';
import type { Command } from './command-stack.js';
import { type Change, diElementsOf, followEnd, layoutCommand, rerouteEdge, subtract } from './di.js';
import type { BpmnDocument, BpmnShapeView, Bounds, Point } from './document.js';

/** The smallest size each resizable kind of shape takes, as the bpmn-js designer sets it. */
const MINIMUM_SIZES: { types: readonly string[]; width: number; height: number; expandedOnly?: boolean }[] = [
    { types: ['bpmn:SubProcess', 'bpmn:Transaction', 'bpmn:AdHocSubProcess'], width: 140, height: 120, expandedOnly: true },
    { types: ['bpmn:TextAnnotation'], width: 50, height: 30 },
    { types: ['bpmn:Group'], width: 50, height: 50 }
];

/**
 * The smallest size of the shape, or `undefined` when it cannot be resized. As in the bpmn-js designer, an expanded
 * subprocess, a text annotation and a group can be resized; a task, an event, a gateway or a collapsed subprocess
 * cannot. Participants and lanes are resized with their lanes, which a later step adds.
 */
export function minimumSize(shape: Pick<BpmnShapeView, 'type' | 'isExpanded'>): { width: number; height: number } | undefined {
    const entry = MINIMUM_SIZES.find(candidate => candidate.types.includes(shape.type));
    if (!entry || (entry.expandedOnly && shape.isExpanded !== true)) return undefined;
    return { width: entry.width, height: entry.height };
}

/**
 * Gives the shape of the element with `elementId` on the plane with `planeId` the bounds `bounds`.
 *
 * - A boundary event attached to it stays on the border it is on, keeping its distance from the nearer corner.
 * - An edge that ends on its border keeps that end on the border, the same way, and stays orthogonal as a move keeps it.
 * - What an expanded subprocess contains stays where it is. The new bounds must still contain it.
 *
 * Positions are computed when the command executes, from the layout at that time. The shape resized is that of the
 * element with `elementId` when the command is created, even if it is renamed before it executes.
 *
 * @throws Error when the document has no such plane, when the element has no shape on it, when the shape cannot be
 * resized, when `bounds` is not finite or smaller than the shape's minimum size, or when it would leave out what the
 * shape contains.
 */
export function resizeShape(document: BpmnDocument, planeId: string, elementId: string, bounds: Bounds): Command {
    if (![bounds.x, bounds.y, bounds.width, bounds.height].every(Number.isFinite)) throw new Error('A resize needs finite bounds.');
    const view = document.planeView(planeId);
    if (!view) throw new Error(`The document has no plane '${planeId}'.`);
    const shape = view.shapes.find(candidate => candidate.elementId === elementId);
    if (!shape) throw new Error(`The plane '${planeId}' has no shape for '${elementId}'.`);
    const minimum = minimumSize(shape);
    if (!minimum) throw new Error(`The shape of '${elementId}' cannot be resized.`);
    if (bounds.width < minimum.width || bounds.height < minimum.height) {
        throw new Error(`The shape of '${elementId}' must be at least ${minimum.width} by ${minimum.height}.`);
    }
    requireContent(view.shapes, elementId, bounds);
    // A copy, so a caller changing it afterwards cannot change what the command applies.
    const target = { x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height };

    const attached = view.shapes.filter(candidate => candidate.attachedToId === elementId).map(candidate => candidate.elementId);
    const ends = new Set([elementId, ...attached]);
    const edges = [...new Set(view.edges.filter(edge => ends.has(edge.sourceId ?? '') || ends.has(edge.targetId ?? '')).map(edge => edge.elementId))];
    // The element, not its id, so an earlier command in the same compound renaming it cannot change what is resized.
    const element = document.element(elementId)!;
    return layoutCommand([elementId, ...attached, ...edges], () => planResize(document, planeId, String(element.id), target));
}

function planResize(document: BpmnDocument, planeId: string, elementId: string, bounds: Bounds): Change[] {
    const view = document.planeView(planeId)!;
    const diById = diElementsOf(document, planeId);
    const shape = view.shapes.find(candidate => candidate.elementId === elementId)!;
    // Checked again here: an earlier command in the same compound can have moved the content since.
    requireContent(view.shapes, elementId, bounds);
    const before = shape.bounds;
    const changes: Change[] = [];
    const diBounds = diById.get(elementId)?.bounds as ModdleElement | undefined;
    if (diBounds) {
        for (const name of ['x', 'y', 'width', 'height'] as const) changes.push({ target: diBounds, name, before: diBounds[name], after: bounds[name] });
    }

    // Each boundary event's centre follows the border it is on; the event keeps its size.
    const deltas = new Map<string, Point>([[elementId, { x: 0, y: 0 }]]);
    for (const event of view.shapes.filter(candidate => candidate.attachedToId === elementId)) {
        const centre = { x: event.bounds.x + event.bounds.width / 2, y: event.bounds.y + event.bounds.height / 2 };
        const delta = subtract(followBorder(centre, before, bounds), centre);
        deltas.set(event.elementId, delta);
        const di = diById.get(event.elementId);
        for (const owned of [di?.bounds, (di?.label as ModdleElement | undefined)?.bounds] as (ModdleElement | undefined)[]) {
            if (!owned) continue;
            changes.push({ target: owned, name: 'x', before: owned.x, after: Number(owned.x ?? 0) + delta.x });
            changes.push({ target: owned, name: 'y', before: owned.y, after: Number(owned.y ?? 0) + delta.y });
        }
    }

    const seen = new Set<string>();
    for (const edge of view.edges) {
        if (seen.has(edge.elementId) || edge.waypoints.length < 2) continue;
        seen.add(edge.elementId);
        const di = diById.get(edge.elementId);
        if (!di) continue;
        let waypoints = edge.waypoints;
        for (const end of ['source', 'target'] as const) {
            const id = end === 'source' ? edge.sourceId : edge.targetId;
            if (id === undefined || !deltas.has(id)) continue;
            const point = end === 'source' ? waypoints[0] : waypoints[waypoints.length - 1];
            // An end on the resized shape follows its border; an end on a boundary event moves with the event.
            const delta = id === elementId ? subtract(followBorder(point, before, bounds), point) : deltas.get(id)!;
            if (delta.x !== 0 || delta.y !== 0) waypoints = followEnd(waypoints, end, delta);
        }
        if (waypoints !== edge.waypoints) changes.push(...rerouteEdge(document, di, edge.waypoints, waypoints));
    }

    return changes;
}

/**
 * Where `point`, on or near the border of `before`, is on the border of `after`: on each axis it keeps its distance from
 * the nearer side, and it stays within the new bounds.
 */
function followBorder(point: Point, before: Bounds, after: Bounds): Point {
    const follow = (value: number, start: number, size: number, newStart: number, newSize: number) => {
        const moved = value - start <= size / 2 ? value + (newStart - start) : value + (newStart + newSize - (start + size));
        return Math.min(Math.max(moved, newStart), newStart + newSize);
    };
    return {
        x: follow(point.x, before.x, before.width, after.x, after.width),
        y: follow(point.y, before.y, before.height, after.y, after.height)
    };
}

/** Throws unless `bounds` contain every shape the element's shape contains, its boundary events aside. */
function requireContent(shapes: readonly BpmnShapeView[], elementId: string, bounds: Bounds): void {
    for (const content of shapes.filter(candidate => candidate.parentId === elementId && candidate.type !== 'bpmn:BoundaryEvent')) {
        if (!contains(bounds, content.bounds)) throw new Error(`The shape of '${elementId}' must still contain '${content.elementId}'.`);
    }
}

function contains(outer: Bounds, inner: Bounds): boolean {
    return inner.x >= outer.x && inner.y >= outer.y && inner.x + inner.width <= outer.x + outer.width && inner.y + inner.height <= outer.y + outer.height;
}
