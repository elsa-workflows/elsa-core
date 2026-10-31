/**
 * Creating an element with its shape, as the bpmn-js designer's palette does: the element goes into the container under
 * the point it is dropped on, its shape is drawn centred on that point at the size bpmn-js gives it, and a flow node is
 * added to the lane it is dropped in.
 */
import type { ModdleElement } from '../binding.js';
import type { Command } from './command-stack.js';
import type { BpmnDocument, BpmnShapeView, Bounds, Point } from './document.js';

/** What can be created, with the size bpmn-js gives each. Participants and subprocesses come in later steps. */
const CREATABLE: Readonly<Record<string, { width: number; height: number }>> = {
    'bpmn:StartEvent': { width: 36, height: 36 },
    'bpmn:IntermediateThrowEvent': { width: 36, height: 36 },
    'bpmn:EndEvent': { width: 36, height: 36 },
    'bpmn:ExclusiveGateway': { width: 50, height: 50 },
    'bpmn:Task': { width: 100, height: 80 },
    'bpmn:DataObjectReference': { width: 36, height: 50 },
    'bpmn:DataStoreReference': { width: 50, height: 50 },
    'bpmn:Group': { width: 300, height: 300 },
    'bpmn:TextAnnotation': { width: 100, height: 30 }
};

/** The id prefixes of what is created with an element of each type: a data object, or a group's category and value. */
const COMPANIONS: Readonly<Record<string, readonly string[]>> = {
    'bpmn:DataObjectReference': ['DataObject'],
    'bpmn:Group': ['Category', 'CategoryValue']
};

/** The element types `createShape` creates. */
export const CREATABLE_TYPES: readonly string[] = Object.keys(CREATABLE);

/** The size `createShape` gives an element of `type`, or `undefined` when it cannot create one. */
export function creatableSize(type: string): { width: number; height: number } | undefined {
    const size = CREATABLE[type];
    return size ? { ...size } : undefined;
}

/**
 * Creates an element of `type` on the plane with `planeId`, its shape centred on `position`, at the size the bpmn-js
 * designer gives it.
 *
 * - It goes into the innermost expanded subprocess or participant whose shape holds `position`, else into what the
 *   plane draws: its process, or the subprocess a collapsed subprocess's plane opens.
 * - A text annotation or group on a collaboration's plane, outside every participant, goes into the collaboration.
 * - A flow node is also referenced by the innermost lane of its process that holds `position`.
 * - A data object reference gets a new data object of its own.
 * - A group gets a new category with one value, in the definitions, as the bpmn-js designer gives it one; the value
 *   holds the group's name.
 * - Ids follow the bpmn-js designer: `Activity_`, `Event_`, `Gateway_` or the type's name, then a random suffix; a
 *   shape's id is its element's with `_di` added.
 *
 * The command's `elementIds` start with the new element's id, followed by the ids of what is created with it. Where
 * the element goes is worked out again when the command executes, from the layout at that time; what it creates is made
 * once, so a redo puts back the objects that later commands refer to.
 *
 * @throws Error when the document has no such plane, when `type` cannot be created here, when `position` is not finite,
 * or when the plane is a collaboration's and `position` is outside every participant (for anything but a text
 * annotation or a group).
 */
export function createShape(document: BpmnDocument, planeId: string, type: string, position: Point): Command {
    const size = CREATABLE[type];
    if (!size) throw new Error(`An element of type '${type}' cannot be created here.`);
    if (!Number.isFinite(position.x) || !Number.isFinite(position.y)) throw new Error('A create needs a finite position.');
    if (!document.planeView(planeId)) throw new Error(`The document has no plane '${planeId}'.`);
    const point = { x: position.x, y: position.y };
    const bounds = { x: Math.round(point.x - size.width / 2), y: Math.round(point.y - size.height / 2), width: size.width, height: size.height };
    // Checked now, so a create that cannot apply is refused before it reaches the stack.
    placementOf(document, planeId, type, point);

    const ids: string[] = [];
    for (const prefix of [idPrefix(document, type), ...(COMPANIONS[type] ?? [])]) ids.push(freshId(document, prefix, ids));
    let created: Created | undefined;
    let operations: Operation[] = [];
    return {
        elementIds: [...ids],
        execute() {
            for (const taken of [...ids, `${ids[0]}_di`]) {
                if (document.hasId(taken)) throw new Error(`The document already has an element '${taken}'.`);
            }
            const placement = placementOf(document, planeId, type, point);
            created ??= make(document, type, ids, bounds);
            operations = planCreate(document, placement, created);
            for (const operation of operations) operation.apply();
            document.reindex();
        },
        revert() {
            for (const operation of [...operations].reverse()) operation.revert();
            document.reindex();
        }
    };
}

interface Operation {
    apply(): void;
    revert(): void;
}

interface Placement {
    plane: ModdleElement;
    /** The process, subprocess or collaboration the element goes into. */
    container: ModdleElement;
    /** The lane that references a flow node, if any. */
    lane?: ModdleElement;
}

/**
 * Where an element of `type` dropped on `point` goes: the innermost expanded subprocess or participant whose shape holds
 * it, else what the plane draws.
 */
function placementOf(document: BpmnDocument, planeId: string, type: string, point: Point): Placement {
    const view = document.planeView(planeId);
    if (!view) throw new Error(`The document has no plane '${planeId}'.`);
    const plane = planeElement(document, planeId)!;
    const holds = (shape: BpmnShapeView) => shape.hiddenBy === undefined && contains(shape.bounds, point);
    const deepest = deepestOf(view.shapes);
    const host = deepest(view.shapes.filter(shape => holds(shape) && shape.isExpanded === true && isContainerType(document, shape.elementId)));

    let container: ModdleElement | undefined;
    if (host) {
        const element = document.element(host.elementId)!;
        container = element.$instanceOf('bpmn:Participant') ? element.processRef as ModdleElement : element;
    } else {
        const drawn = plane.bpmnElement as ModdleElement | undefined;
        if (drawn?.$instanceOf('bpmn:Collaboration')) {
            if (!isA(document, type, 'bpmn:Artifact')) throw new Error('On a collaboration, an element must be created inside a participant.');
            container = drawn;
        } else if (drawn?.$instanceOf('bpmn:Process') || drawn?.$instanceOf('bpmn:SubProcess')) {
            container = drawn;
        }
    }
    if (!container) throw new Error(`The plane '${planeId}' has nowhere to create an element.`);

    // The innermost lane of the container that holds the point.
    const laneShape = isA(document, type, 'bpmn:FlowNode') && container.$instanceOf('bpmn:Process')
        ? deepest(view.shapes.filter(shape => shape.type === 'bpmn:Lane' && holds(shape) && processOfLane(document.element(shape.elementId)!) === container))
        : undefined;
    return { plane, container, lane: laneShape ? document.element(laneShape.elementId) : undefined };
}

/**
 * Picks, of shapes on a plane, the one with the most shapes of `shapes` around it, through each shape's `parentId`; the
 * last such one in document order when there are several. The DI's order is not trusted to put a container first.
 */
function deepestOf(shapes: readonly BpmnShapeView[]): (candidates: readonly BpmnShapeView[]) => BpmnShapeView | undefined {
    const byId = new Map(shapes.map(shape => [shape.elementId, shape]));
    const depth = (shape: BpmnShapeView) => {
        let count = 0;
        for (let parent = shape.parentId; parent !== undefined && count < shapes.length; parent = byId.get(parent)?.parentId) count++;
        return count;
    };
    return candidates => candidates.reduce<BpmnShapeView | undefined>((best, shape) => (!best || depth(shape) >= depth(best) ? shape : best), undefined);
}

/**
 * What a create adds: the element, the data object of a data object reference, the category of a group, and the
 * element's shape.
 */
interface Created {
    element: ModdleElement;
    dataObject?: ModdleElement;
    category?: ModdleElement;
    shape: ModdleElement;
}

function make(document: BpmnDocument, type: string, ids: readonly string[], bounds: Bounds): Created {
    const { moddle } = document;
    const [id, ...companions] = ids;
    const element = moddle.create(type, { id });
    const dataObject = type === 'bpmn:DataObjectReference' ? moddle.create('bpmn:DataObject', { id: companions[0] }) : undefined;
    if (dataObject) element.dataObjectRef = dataObject;
    let category: ModdleElement | undefined;
    if (type === 'bpmn:Group') {
        category = moddle.create('bpmn:Category', { id: companions[0] });
        const value = moddle.create('bpmn:CategoryValue', { id: companions[1] });
        value.$parent = category;
        category.categoryValue = [value];
        element.categoryValueRef = value;
    }
    const shape = moddle.create('bpmndi:BPMNShape', { id: `${id}_di`, bpmnElement: element });
    const shapeBounds = moddle.create('dc:Bounds', { ...bounds });
    shapeBounds.$parent = shape;
    shape.bounds = shapeBounds;
    return { element, dataObject, category, shape };
}

function planCreate(document: BpmnDocument, placement: Placement, { element, dataObject, category, shape }: Created): Operation[] {
    const operations: Operation[] = [];
    operations.push(append(placement.container, element.$instanceOf('bpmn:Artifact') ? 'artifacts' : 'flowElements', element));
    if (dataObject) operations.push(append(placement.container, 'flowElements', dataObject));
    if (category) operations.push(append(document.definitions, 'rootElements', category));
    if (placement.lane) operations.push(append(placement.lane, 'flowNodeRef', element, false));
    operations.push(append(placement.plane, 'planeElement', shape));
    return operations;
}

/**
 * Adds `child` at the end of the collection `name` of `owner`, creating the collection when the owner has none, and
 * takes it out again on revert. A contained child gets `owner` as its parent; a referenced one keeps its own.
 */
function append(owner: ModdleElement, name: string, child: ModdleElement, contains = true): Operation {
    let created = false;
    return {
        apply() {
            created = !Array.isArray(owner[name]);
            const collection = (owner.get as (name: string) => ModdleElement[])(name);
            collection.push(child);
            if (contains) child.$parent = owner;
        },
        revert() {
            const collection = owner[name] as ModdleElement[];
            const index = collection.lastIndexOf(child);
            if (index >= 0) collection.splice(index, 1);
            if (created) (owner.set as (name: string, value: unknown) => void)(name, undefined);
        }
    };
}

/** The id prefix bpmn-js gives an element of `type`. */
function idPrefix(document: BpmnDocument, type: string): string {
    for (const [base, prefix] of [['bpmn:Activity', 'Activity'], ['bpmn:Event', 'Event'], ['bpmn:Gateway', 'Gateway']]) {
        if (isA(document, type, base)) return prefix;
    }
    return type.replace(/^[^:]*:/, '');
}

/** Whether `type` is `base` or derives from it. */
function isA(document: BpmnDocument, type: string, base: string): boolean {
    return document.moddle.create(type).$instanceOf(base);
}

/** `prefix_` and seven random base-36 characters, as bpmn-js generates ids, that no element and no shape has. */
function freshId(document: BpmnDocument, prefix: string, taken: readonly string[] = []): string {
    for (;;) {
        const suffix = Math.floor(Math.random() * 36 ** 7).toString(36).padStart(7, '0');
        const id = `${prefix}_${suffix}`;
        if (!document.hasId(id) && !document.hasId(`${id}_di`) && !taken.includes(id)) return id;
    }
}

function planeElement(document: BpmnDocument, planeId: string): ModdleElement | undefined {
    const diagrams = (document.definitions.diagrams as ModdleElement[] | undefined) ?? [];
    return diagrams.map(diagram => diagram.plane as ModdleElement | undefined).find(plane => plane?.id === planeId);
}

function isContainerType(document: BpmnDocument, elementId: string): boolean {
    const element = document.element(elementId);
    return element !== undefined && (element.$instanceOf('bpmn:SubProcess') || (element.$instanceOf('bpmn:Participant') && element.processRef !== undefined));
}

/** The process a lane's lane set belongs to, through any lanes that contain it. */
function processOfLane(lane: ModdleElement): ModdleElement | undefined {
    let current = lane.$parent as ModdleElement | undefined;
    while (current && !current.$instanceOf('bpmn:Process')) current = current.$parent as ModdleElement | undefined;
    return current;
}

function contains(bounds: Bounds, point: Point): boolean {
    return point.x >= bounds.x && point.x <= bounds.x + bounds.width && point.y >= bounds.y && point.y <= bounds.y + bounds.height;
}
