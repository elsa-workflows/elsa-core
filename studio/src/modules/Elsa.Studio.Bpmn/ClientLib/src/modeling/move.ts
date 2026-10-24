/**
 * Moving shapes on a plane, with what follows them: their labels, what they contain, the boundary events attached to
 * them, and the edges that connect them. A move changes layout (DI) only; it never changes which container an element
 * belongs to.
 */
import type { ModdleElement } from '../binding.js';
import type { Command } from './command-stack.js';
import { add, type Change, diElementsOf, followEnd, layoutCommand, rerouteEdge, translateBounds } from './di.js';
import type { BpmnDocument, BpmnEdgeView, BpmnShapeView, Point } from './document.js';

/**
 * Moves the shapes of the elements with `elementIds` on the plane with `planeId` by `delta`.
 *
 * - The shapes they contain and the boundary events attached to them move with them, and so do all of their labels.
 * - An edge between two moved shapes moves with them.
 * - An edge with one moved end keeps its other end. The moved end follows, and the segment next to it stays horizontal
 *   or vertical when it was: the next bend point follows it, and a straight two-point edge gets two bend points.
 * - An edge's label keeps its offset from the edge's midpoint.
 *
 * Positions are computed when the command executes, from the layout at that time. The shapes moved are those of the
 * elements with `elementIds` when the command is created, even if they are renamed before it executes.
 *
 * @throws Error when the document has no such plane, when an element has no shape on it, when an element is a lane or
 * a boundary event (a lane moves with its participant, a boundary event with its activity), or when `delta` is not
 * finite.
 */
export function moveShapes(document: BpmnDocument, planeId: string, elementIds: readonly string[], delta: Point): Command {
    if (!Number.isFinite(delta.x) || !Number.isFinite(delta.y)) throw new Error('A move needs a finite delta.');
    const view = document.planeView(planeId);
    if (!view) throw new Error(`The document has no plane '${planeId}'.`);
    if (elementIds.length === 0) throw new Error('A move needs at least one element.');
    const shapes = new Map(view.shapes.map(shape => [shape.elementId, shape]));
    for (const elementId of elementIds) {
        const shape = shapes.get(elementId);
        if (!shape) throw new Error(`The plane '${planeId}' has no shape for '${elementId}'.`);
        if (shape.type === 'bpmn:Lane') throw new Error(`The lane '${elementId}' moves with its participant.`);
        if (shape.type === 'bpmn:BoundaryEvent' && !elementIds.includes(shape.attachedToId ?? '')) {
            throw new Error(`The boundary event '${elementId}' moves with the activity it is attached to.`);
        }
    }
    // The elements, not their ids, so a caller changing the array afterwards, or an earlier command in the same compound
    // renaming one, cannot change what the command moves.
    const elements = elementIds.map(id => document.element(id)!);
    const { x: dx, y: dy } = delta;
    const { shapes: moved, edges } = affected(view.shapes, view.edges, elementIds);

    return layoutCommand([...moved, ...edges.keys()], () => planMove(document, planeId, elements.map(element => String(element.id)), { x: dx, y: dy }));
}

function planMove(document: BpmnDocument, planeId: string, elementIds: readonly string[], delta: Point): Change[] {
    const view = document.planeView(planeId)!;
    const diById = diElementsOf(document, planeId);
    const { shapes: moved, edges } = affected(view.shapes, view.edges, elementIds);
    const changes: Change[] = [];

    for (const elementId of moved) {
        const di = diById.get(elementId);
        if (!di) continue;
        changes.push(...translateBounds(di.bounds as ModdleElement | undefined, delta));
        changes.push(...translateBounds((di.label as ModdleElement | undefined)?.bounds as ModdleElement | undefined, delta));
    }

    for (const edge of edges.values()) {
        const sourceMoved = edge.sourceId !== undefined && moved.has(edge.sourceId);
        const targetMoved = edge.targetId !== undefined && moved.has(edge.targetId);
        const di = diById.get(edge.elementId);
        if (!di || edge.waypoints.length < 2) continue;

        const waypoints = sourceMoved && targetMoved ? edge.waypoints.map(point => add(point, delta)) : followEnd(edge.waypoints, sourceMoved ? 'source' : 'target', delta);
        changes.push(...rerouteEdge(document, di, edge.waypoints, waypoints));
    }

    return changes;
}

/**
 * What a move of `elementIds` changes: the shapes that move (see `movedShapes`), and the first drawing of each edge with
 * a moved end, by element id.
 */
function affected(shapes: readonly BpmnShapeView[], edges: readonly BpmnEdgeView[], elementIds: readonly string[]) {
    const moved = movedShapes(shapes, elementIds);
    const changed = new Map<string, BpmnEdgeView>();
    for (const edge of edges) {
        const hasMovedEnd = [edge.sourceId, edge.targetId].some(id => id !== undefined && moved.has(id));
        if (hasMovedEnd && !changed.has(edge.elementId)) changed.set(edge.elementId, edge);
    }
    return { shapes: moved, edges: changed };
}

/** The elements given, what they contain at any depth, and the boundary events attached to any of them. */
function movedShapes(shapes: readonly BpmnShapeView[], elementIds: readonly string[]): Set<string> {
    const moved = new Set(elementIds);
    for (let grew = true; grew;) {
        grew = false;
        for (const shape of shapes) {
            if (moved.has(shape.elementId)) continue;
            const follows = (shape.parentId !== undefined && moved.has(shape.parentId)) || (shape.attachedToId !== undefined && moved.has(shape.attachedToId));
            if (follows) {
                moved.add(shape.elementId);
                grew = true;
            }
        }
    }
    return moved;
}
