/**
 * The planes the designer shows, one at a time: the document's own diagram planes, and a plane of its own for each
 * collapsed subprocess whose content an older tool drew on its parent's plane (`hiddenBy`), as bpmn-js shows it. That
 * content stays where the document has it; only the designer shows it on the subprocess's own page.
 */
import type { BpmnDocument, BpmnEdgeView, BpmnShapeView } from '../../../Elsa.Studio.Bpmn/ClientLib/src/modeling/index.js';

const SUBPROCESS_TYPES = new Set(['bpmn:SubProcess', 'bpmn:Transaction', 'bpmn:AdHocSubProcess']);

export interface ShownPlane {
    /** The diagram plane's id, or `<planeId>/<subprocessId>` for the content a collapsed subprocess hides on that plane. */
    readonly key: string;
    /** What the breadcrumb shows: the subprocess's name for a subprocess's plane, else the diagram's or its element's. */
    readonly title: string;
    /** For a subprocess's plane, the plane its collapsed shape is drawn on. */
    readonly parentKey?: string;
    /** For a subprocess's plane, the subprocess it opens. */
    readonly subprocessId?: string;
    readonly shapes: readonly BpmnShapeView[];
    readonly edges: readonly BpmnEdgeView[];
}

export interface PlaneIndex {
    readonly planes: readonly ShownPlane[];
    /** The first plane with no parent: what the designer shows after a load. */
    readonly rootKey: string | undefined;
    plane(key: string): ShownPlane | undefined;
    /** The plane that first draws the element with `elementId`, or `undefined` when no plane draws it. */
    planeOfElement(elementId: string): string | undefined;
    /** The plane a collapsed subprocess opens, or `undefined` when it has nothing to show. */
    drillTarget(subprocessId: string): string | undefined;
    /** The planes from the root down to the one with `key`, inclusive. */
    path(key: string): ShownPlane[];
}

export function indexPlanes(document: BpmnDocument): PlaneIndex {
    const planes: ShownPlane[] = [];
    const subprocessPlanes: { planeId: string; elementId: string }[] = [];

    for (const plane of document.planes()) {
        const view = document.planeView(plane.planeId);
        if (!view) continue;
        const subprocess = plane.elementId && SUBPROCESS_TYPES.has(String(document.element(plane.elementId)?.$type)) ? plane.elementId : undefined;
        if (subprocess) subprocessPlanes.push({ planeId: plane.planeId, elementId: subprocess });

        const owners = ownersOf(view.shapes, view.edges);
        const groups = new Map<string | undefined, { shapes: BpmnShapeView[]; edges: BpmnEdgeView[] }>();
        const group = (owner: string | undefined) => {
            let found = groups.get(owner);
            if (!found) groups.set(owner, (found = { shapes: [], edges: [] }));
            return found;
        };
        group(undefined);
        for (const shape of view.shapes) group(owners.shapes.get(shape)).shapes.push(shape);
        for (const edge of view.edges) group(owners.edges.get(edge)).edges.push(edge);

        const ownPlane = groups.get(undefined)!;
        planes.push({
            key: plane.planeId,
            title: titleOf(document, subprocess ?? plane.elementId, plane.name ?? plane.planeId),
            subprocessId: subprocess,
            shapes: ownPlane.shapes,
            edges: ownPlane.edges
        });
        for (const [owner, content] of groups) {
            if (owner === undefined) continue;
            planes.push({
                key: `${plane.planeId}/${owner}`,
                title: titleOf(document, owner, owner),
                subprocessId: owner,
                shapes: content.shapes,
                edges: content.edges
            });
        }
    }

    const planeOfElement = new Map<string, string>();
    for (const plane of planes) {
        for (const drawn of [...plane.shapes, ...plane.edges]) {
            if (!planeOfElement.has(drawn.elementId)) planeOfElement.set(drawn.elementId, plane.key);
        }
    }

    // A collapsed subprocess opens its own diagram plane when the document has one, else the content drawn hidden.
    const drillTargets = new Map<string, string>();
    for (const plane of planes) {
        if (plane.subprocessId && plane.key.includes('/') && !drillTargets.has(plane.subprocessId)) drillTargets.set(plane.subprocessId, plane.key);
    }
    for (const { planeId, elementId } of subprocessPlanes) drillTargets.set(elementId, planeId);

    const byKey = new Map<string, ShownPlane>();
    for (const plane of planes) {
        const parentKey = plane.subprocessId ? planeOfElement.get(plane.subprocessId) : undefined;
        byKey.set(plane.key, parentKey && parentKey !== plane.key ? { ...plane, parentKey } : plane);
    }
    const indexed = [...byKey.values()];

    return {
        planes: indexed,
        rootKey: indexed.find(plane => !plane.parentKey)?.key,
        plane: key => byKey.get(key),
        planeOfElement: elementId => planeOfElement.get(elementId),
        drillTarget: subprocessId => drillTargets.get(subprocessId),
        path(key) {
            const path: ShownPlane[] = [];
            const seen = new Set<string>();
            for (let plane = byKey.get(key); plane && !seen.has(plane.key); plane = plane.parentKey ? byKey.get(plane.parentKey) : undefined) {
                seen.add(plane.key);
                path.unshift(plane);
            }
            return path;
        }
    };
}

/**
 * The collapsed subprocess each shape and edge belongs to, if any: the nearest one among the shape's containers, so
 * the content of a collapsed subprocess nested in hidden content goes to the inner subprocess's plane.
 */
function ownersOf(shapes: readonly BpmnShapeView[], edges: readonly BpmnEdgeView[]) {
    const byElementId = new Map<string, BpmnShapeView>();
    for (const shape of shapes) {
        if (!byElementId.has(shape.elementId)) byElementId.set(shape.elementId, shape);
    }
    const isCollapsedSubprocess = (shape: BpmnShapeView | undefined) => !!shape && shape.isExpanded === false && SUBPROCESS_TYPES.has(shape.type);

    const shapeOwners = new Map<BpmnShapeView, string>();
    for (const shape of shapes) {
        if (!shape.hiddenBy) continue;
        let owner: string | undefined;
        const seen = new Set<string>();
        for (let parent = shape.parentId ? byElementId.get(shape.parentId) : undefined; parent && !seen.has(parent.elementId); parent = parent.parentId ? byElementId.get(parent.parentId) : undefined) {
            seen.add(parent.elementId);
            if (isCollapsedSubprocess(parent)) {
                owner = parent.elementId;
                break;
            }
        }
        shapeOwners.set(shape, owner ?? shape.hiddenBy);
    }

    const edgeOwners = new Map<BpmnEdgeView, string>();
    for (const edge of edges) {
        if (!edge.hiddenBy) continue;
        const end = [edge.sourceId, edge.targetId].map(id => (id ? byElementId.get(id) : undefined)).find(shape => shape && shapeOwners.has(shape));
        edgeOwners.set(edge, (end && shapeOwners.get(end)) ?? edge.hiddenBy);
    }

    return { shapes: shapeOwners, edges: edgeOwners };
}

function titleOf(document: BpmnDocument, elementId: string | null | undefined, fallback: string): string {
    const name = elementId ? document.element(elementId)?.name : undefined;
    return typeof name === 'string' && name.trim() !== '' ? name : (elementId ?? fallback);
}
