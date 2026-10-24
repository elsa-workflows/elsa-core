/**
 * Moving shapes on a plane, with what follows them: their labels, what they contain, the boundary events attached to
 * them, and the edges that connect them. A move changes layout (DI) only; it never changes which container an element
 * belongs to.
 */
import type { ModdleElement } from '../binding.js';
import type { Command } from './command-stack.js';
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
 * Positions are computed when the command executes, from the layout at that time.
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
    // Copies, so a caller changing them afterwards cannot change what the command moves.
    const ids = [...elementIds];
    const { x: dx, y: dy } = delta;

    const { shapes: moved, edges } = affected(view.shapes, view.edges, ids);

    let changes: Change[] = [];
    return {
        elementIds: [...moved, ...edges.keys()],
        execute() {
            changes = planMove(document, planeId, ids, { x: dx, y: dy });
            for (const change of changes) change.target[change.name] = change.after;
        },
        revert() {
            for (const change of [...changes].reverse()) change.target[change.name] = change.before;
        }
    };
}

/** One property of one DI element, as it is before and after the move. */
interface Change {
    target: ModdleElement;
    name: string;
    before: unknown;
    after: unknown;
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

        const waypoints = sourceMoved && targetMoved ? edge.waypoints.map(point => add(point, delta)) : followEnd(edge, sourceMoved ? 'source' : 'target', delta);
        changes.push({ target: di, name: 'waypoint', before: di.waypoint, after: waypoints.map(({ x, y }) => document.moddle.create('dc:Point', { x, y })) });
        const labelBounds = (di.label as ModdleElement | undefined)?.bounds as ModdleElement | undefined;
        changes.push(...translateBounds(labelBounds, subtract(midpointOf(waypoints), midpointOf(edge.waypoints))));
    }

    return changes;
}

/** The first DI element on the plane for each element it draws, by element id, as the plane view takes them. */
function diElementsOf(document: BpmnDocument, planeId: string): Map<string, ModdleElement> {
    const diagrams = (document.definitions.diagrams as ModdleElement[] | undefined) ?? [];
    const plane = diagrams.map(diagram => diagram.plane as ModdleElement | undefined).find(candidate => candidate?.id === planeId);
    const byId = new Map<string, ModdleElement>();
    for (const di of (plane?.planeElement as ModdleElement[] | undefined) ?? []) {
        const id = (di.bpmnElement as ModdleElement | undefined)?.id;
        if (typeof id === 'string' && !byId.has(id)) byId.set(id, di);
    }
    return byId;
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

function translateBounds(bounds: ModdleElement | undefined, delta: Point): Change[] {
    if (!bounds || (delta.x === 0 && delta.y === 0)) return [];
    return [
        { target: bounds, name: 'x', before: bounds.x, after: Number(bounds.x ?? 0) + delta.x },
        { target: bounds, name: 'y', before: bounds.y, after: Number(bounds.y ?? 0) + delta.y }
    ];
}

/**
 * The waypoints of `edge` with its `end` moved by `delta`. The segment at that end stays horizontal or vertical when it
 * was: the bend point after it follows on the one axis that keeps it so, and a straight two-point edge gets two bend
 * points halfway, so it stays orthogonal.
 */
function followEnd(edge: BpmnEdgeView, end: 'source' | 'target', delta: Point): Point[] {
    const points = end === 'source' ? [...edge.waypoints] : [...edge.waypoints].reverse();
    const [first, second] = points;
    const horizontal = first.y === second.y && first.x !== second.x;
    const vertical = first.x === second.x && first.y !== second.y;
    const moved = add(first, delta);
    let result: Point[];

    if (points.length > 2) {
        const next = horizontal ? { x: second.x, y: second.y + delta.y } : vertical ? { x: second.x + delta.x, y: second.y } : second;
        result = [moved, next, ...points.slice(2)];
    } else if (horizontal && delta.y !== 0) {
        const middle = (moved.x + second.x) / 2;
        result = [moved, { x: middle, y: moved.y }, { x: middle, y: second.y }, second];
    } else if (vertical && delta.x !== 0) {
        const middle = (moved.y + second.y) / 2;
        result = [moved, { x: moved.x, y: middle }, { x: second.x, y: middle }, second];
    } else {
        result = [moved, second];
    }

    return end === 'source' ? result : result.reverse();
}

/** The point halfway along a path, by length. */
function midpointOf(points: readonly Point[]): Point {
    const lengths = points.slice(1).map((point, index) => Math.hypot(point.x - points[index].x, point.y - points[index].y));
    let remaining = lengths.reduce((sum, length) => sum + length, 0) / 2;
    for (let index = 0; index < lengths.length; index++) {
        if (remaining <= lengths[index] && lengths[index] > 0) {
            const ratio = remaining / lengths[index];
            const [from, to] = [points[index], points[index + 1]];
            return { x: from.x + (to.x - from.x) * ratio, y: from.y + (to.y - from.y) * ratio };
        }
        remaining -= lengths[index];
    }
    return points[0];
}

function add(point: Point, delta: Point): Point {
    return { x: point.x + delta.x, y: point.y + delta.y };
}

function subtract(point: Point, other: Point): Point {
    return { x: point.x - other.x, y: point.y - other.y };
}
