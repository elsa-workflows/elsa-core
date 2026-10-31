/**
 * Layout (DI) edits shared by the commands that move and resize shapes: the DI elements of a plane, and changes to their
 * properties that a command applies and puts back.
 */
import type { ModdleElement } from '../binding.js';
import type { Command } from './command-stack.js';
import type { BpmnDocument, Point } from './document.js';

/** One property of one DI element, as it is before and after an edit. */
export interface Change {
    target: ModdleElement;
    name: string;
    before: unknown;
    after: unknown;
}

/**
 * A command that applies the changes `plan` computes when it executes, from the layout at that time, and puts back
 * exactly what they replaced.
 */
export function layoutCommand(elementIds: readonly string[], plan: () => Change[]): Command {
    let changes: Change[] = [];
    return {
        elementIds,
        execute() {
            changes = plan();
            for (const change of changes) change.target[change.name] = change.after;
        },
        revert() {
            for (const change of [...changes].reverse()) change.target[change.name] = change.before;
        }
    };
}

/** The first DI element on the plane for each element it draws, by element id, as the plane view takes them. */
export function diElementsOf(document: BpmnDocument, planeId: string): Map<string, ModdleElement> {
    const diagrams = (document.definitions.diagrams as ModdleElement[] | undefined) ?? [];
    const plane = diagrams.map(diagram => diagram.plane as ModdleElement | undefined).find(candidate => candidate?.id === planeId);
    const byId = new Map<string, ModdleElement>();
    for (const di of (plane?.planeElement as ModdleElement[] | undefined) ?? []) {
        const id = (di.bpmnElement as ModdleElement | undefined)?.id;
        if (typeof id === 'string' && !byId.has(id)) byId.set(id, di);
    }
    return byId;
}

/** Changes an edge's waypoints to `waypoints`, its label keeping its offset from the edge's midpoint. */
export function rerouteEdge(document: BpmnDocument, di: ModdleElement, before: readonly Point[], waypoints: readonly Point[]): Change[] {
    const labelBounds = (di.label as ModdleElement | undefined)?.bounds as ModdleElement | undefined;
    return [
        { target: di, name: 'waypoint', before: di.waypoint, after: waypointElements(document, (di.waypoint as ModdleElement[] | undefined) ?? [], waypoints) },
        ...translateBounds(labelBounds, subtract(midpointOf(waypoints), midpointOf(before)))
    ];
}

/**
 * The `dc:Point` elements for `waypoints`, each keeping the foreign attributes of the point it replaces: point for point
 * when the count is unchanged, else the two ends, the bend points added between them having none.
 */
function waypointElements(document: BpmnDocument, original: readonly ModdleElement[], waypoints: readonly Point[]): ModdleElement[] {
    return waypoints.map(({ x, y }, index) => {
        const replaced = waypoints.length === original.length ? original[index]
            : index === 0 ? original[0]
            : index === waypoints.length - 1 ? original[original.length - 1]
            : undefined;
        const point = document.moddle.create('dc:Point', { x, y });
        Object.assign(point.$attrs as Record<string, unknown>, (replaced?.$attrs as Record<string, unknown> | undefined) ?? {});
        return point;
    });
}

export function translateBounds(bounds: ModdleElement | undefined, delta: Point): Change[] {
    if (!bounds || (delta.x === 0 && delta.y === 0)) return [];
    return [
        { target: bounds, name: 'x', before: bounds.x, after: Number(bounds.x ?? 0) + delta.x },
        { target: bounds, name: 'y', before: bounds.y, after: Number(bounds.y ?? 0) + delta.y }
    ];
}

/**
 * `points`, an edge's waypoints, with its `end` moved by `delta`. The segment at that end stays horizontal or vertical when it
 * was: the bend point after it follows on the one axis that keeps it so, and a straight two-point edge gets two bend
 * points halfway, so it stays orthogonal.
 */
export function followEnd(waypoints: readonly Point[], end: 'source' | 'target', delta: Point): Point[] {
    const points = end === 'source' ? [...waypoints] : [...waypoints].reverse();
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
export function midpointOf(points: readonly Point[]): Point {
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

export function add(point: Point, delta: Point): Point {
    return { x: point.x + delta.x, y: point.y + delta.y };
}

export function subtract(point: Point, other: Point): Point {
    return { x: point.x - other.x, y: point.y - other.y };
}
