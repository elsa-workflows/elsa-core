/**
 * A BPMN document as the modeling kernel holds it: the bpmn-moddle object tree, an index of its elements by id, and the
 * diagram layout (DI) of each of its planes, in the terms a canvas draws.
 *
 * Reading and writing go through the same moddle and options the bpmn-js designer uses, so what a save writes matches
 * what that designer writes for the same object tree. Unlike bpmn-js, reading changes nothing in the tree: a collapsed
 * subprocess whose content is drawn on its parent's plane, as older tools write it, keeps that layout. The plane view
 * marks such content as hidden instead.
 *
 * Needs a global `DOMParser` and `XMLSerializer`, as every browser has.
 */
import type { ModdleElement } from '../binding.js';
import type { BpmnLoadWarning } from '../contract.js';
import { withGeneratedLayout } from '../layout.js';
import { computeLoadWarnings } from '../load-warnings.js';
import { createModdle, type Moddle } from './moddle.js';

export interface Bounds {
    x: number;
    y: number;
    width: number;
    height: number;
}

export interface Point {
    x: number;
    y: number;
}

/** A diagram plane: what one canvas page draws. */
export interface BpmnPlane {
    planeId: string;
    diagramId: string;
    /** The element the plane draws: a process, a collaboration, or the subprocess a collapsed subprocess opens. */
    elementId: string | null;
    /** The diagram's name. */
    name?: string;
}

/** A shape on a plane, with the element it draws. */
export interface BpmnShapeView {
    diId: string;
    elementId: string;
    /** The element's moddle type, such as `bpmn:UserTask`. */
    type: string;
    name?: string;
    bounds: Bounds;
    label?: Bounds;
    /** Set for a subprocess, a call activity or a participant: whether the shape draws its content. */
    isExpanded?: boolean;
    /** For a boundary event, the activity it is attached to. */
    attachedToId?: string;
    /** The shape on this plane that contains this one: its expanded subprocess, else the participant of its process. */
    parentId?: string;
    /** The collapsed subprocess on this plane that hides this shape, when an older tool drew its content here. */
    hiddenBy?: string;
}

/** An edge on a plane, with the element it draws. */
export interface BpmnEdgeView {
    diId: string;
    elementId: string;
    type: string;
    name?: string;
    waypoints: Point[];
    label?: Bounds;
    /** The element the edge leaves. */
    sourceId?: string;
    /** The element the edge enters. */
    targetId?: string;
    /** As for a shape. */
    hiddenBy?: string;
}

export interface BpmnPlaneView {
    plane: BpmnPlane;
    /** Every shape the plane holds, in document order, so a container comes before what it contains. */
    shapes: BpmnShapeView[];
    /** Every edge the plane holds, in document order. */
    edges: BpmnEdgeView[];
}

export interface BpmnReadResult {
    document: BpmnDocument;
    /** What a save right after reading loses or rewrites, and why a layout was generated. */
    warnings: BpmnLoadWarning[];
}

interface IndexedPlane {
    readonly plane: BpmnPlane;
    readonly element: ModdleElement;
}

export class BpmnDocument {
    private elements = new Map<string, ModdleElement>();
    private ids = new Set<string>();
    private indexedPlanes: IndexedPlane[] = [];
    private planeIdByElementId = new Map<string, string>();

    private constructor(
        /** The moddle that read the document, for creating elements in it. */
        readonly moddle: Moddle,
        /** The `bpmn:Definitions` root. */
        readonly definitions: ModdleElement
    ) {
        this.reindex();
    }

    /**
     * Reads `xml`, generating a layout when it has none, and reports what a save right after reading would lose or
     * rewrite, the way the bpmn-js designer does.
     *
     * @throws Error when `xml` is not a BPMN document.
     */
    static async read(xml: string): Promise<BpmnReadResult> {
        const layout = await withGeneratedLayout(xml);
        const moddle = createModdle();
        const { rootElement } = await moddle.fromXML(layout.xml);
        const document = new BpmnDocument(moddle, rootElement);
        const warnings = [...computeLoadWarnings(xml, await document.write()), ...layout.warnings];
        return { document, warnings };
    }

    /** The document as XML. The tree is serialized when this is called, so a later edit is not in the result. */
    write(): Promise<string> {
        return this.moddle.toXML(this.definitions, { format: true }).then(result => result.xml);
    }

    /** The semantic element with `id`, or `undefined`. Diagram elements are not included. */
    element(id: string): ModdleElement | undefined {
        return this.elements.get(id);
    }

    /** Whether any element of the document, semantic or diagram, has `id`. */
    hasId(id: string): boolean {
        return this.ids.has(id);
    }

    /** Every diagram plane, in document order. */
    planes(): BpmnPlane[] {
        return this.indexedPlanes.map(indexed => ({ ...indexed.plane }));
    }

    /** The plane that draws the element with `id`, or `undefined` when no plane does. */
    planeOfElement(id: string): string | undefined {
        return this.planeIdByElementId.get(id);
    }

    /**
     * What the plane with `planeId` draws, or `undefined` for an unknown plane. A shape or edge whose element the
     * document does not declare is left out: there is nothing for it to draw.
     */
    planeView(planeId: string): BpmnPlaneView | undefined {
        const indexed = this.indexedPlanes.find(candidate => candidate.plane.planeId === planeId);
        if (!indexed) return undefined;

        const diElements = ((indexed.element.planeElement as ModdleElement[] | undefined) ?? []).filter(di => drawnElement(di));
        const shapeDis = diElements.filter(di => di.$instanceOf('bpmndi:BPMNShape'));
        const shapeIdsOnPlane = new Map(shapeDis.map(di => [drawnElement(di)!, di]));
        const shapes: BpmnShapeView[] = [];
        const edges: BpmnEdgeView[] = [];

        for (const di of diElements) {
            const element = di.bpmnElement as ModdleElement;
            const context = placement(element, shapeIdsOnPlane);
            if (di.$instanceOf('bpmndi:BPMNShape')) {
                shapes.push(withoutUndefined({
                    diId: String(di.id),
                    elementId: String(element.id),
                    type: element.$type,
                    name: nameOf(element),
                    bounds: boundsOf(di.bounds as ModdleElement),
                    label: labelOf(di),
                    isExpanded: hasExpandedState(element) ? isExpanded(element, di) : undefined,
                    attachedToId: idOf(element.attachedToRef),
                    parentId: context.parentId,
                    hiddenBy: context.hiddenBy
                }));
            } else if (di.$instanceOf('bpmndi:BPMNEdge')) {
                const waypoints = (di.waypoint as ModdleElement[] | undefined) ?? [];
                edges.push(withoutUndefined({
                    diId: String(di.id),
                    elementId: String(element.id),
                    type: element.$type,
                    name: nameOf(element),
                    waypoints: waypoints.map(point => ({ x: Number(point.x), y: Number(point.y) })),
                    label: labelOf(di),
                    sourceId: endOf(element, 'source'),
                    targetId: endOf(element, 'target'),
                    hiddenBy: context.hiddenBy
                }));
            }
        }

        return { plane: { ...indexed.plane }, shapes, edges };
    }

    /**
     * Rebuilds the indexes from the tree. A command that changes an id, or adds or removes elements, calls this after it
     * executes and after it reverts.
     */
    reindex(): void {
        const elements = new Map<string, ModdleElement>();
        const ids = new Set<string>();
        // The first element with an id wins, as when bpmn-moddle resolves a reference to a duplicated id.
        walk(this.definitions, element => {
            if (typeof element.id !== 'string') return;
            ids.add(element.id);
            const isDiagram = element.$type.startsWith('bpmndi:');
            if (!isDiagram && !elements.has(element.id)) elements.set(element.id, element);
        });

        const indexedPlanes: IndexedPlane[] = [];
        const planeIdByElementId = new Map<string, string>();
        for (const diagram of (this.definitions.diagrams as ModdleElement[] | undefined) ?? []) {
            const plane = diagram.plane as ModdleElement | undefined;
            if (!plane) continue;
            const planeId = String(plane.id);
            indexedPlanes.push({
                element: plane,
                plane: withoutUndefined({ planeId, diagramId: String(diagram.id), elementId: drawnElement(plane) ?? null, name: typeof diagram.name === 'string' ? diagram.name : undefined })
            });
            for (const di of (plane.planeElement as ModdleElement[] | undefined) ?? []) {
                const elementId = drawnElement(di);
                if (elementId && !planeIdByElementId.has(elementId)) planeIdByElementId.set(elementId, planeId);
            }
        }

        this.elements = elements;
        this.ids = ids;
        this.indexedPlanes = indexedPlanes;
        this.planeIdByElementId = planeIdByElementId;
    }
}

/** Calls `visit` for `element` and every element it contains, following containment and never references. */
function walk(element: ModdleElement, visit: (element: ModdleElement) => void): void {
    visit(element);
    const descriptor = element.$descriptor as { properties: { name: string; isReference?: boolean }[] } | undefined;
    for (const property of descriptor?.properties ?? []) {
        if (property.isReference) continue;
        const value = element[property.name];
        for (const child of Array.isArray(value) ? value : [value]) {
            if (isModdleElement(child)) walk(child, visit);
        }
    }
}

function isModdleElement(value: unknown): value is ModdleElement {
    return typeof value === 'object' && value !== null && typeof (value as ModdleElement).$instanceOf === 'function';
}

function drawnElement(di: ModdleElement): string | undefined {
    return idOf(di.bpmnElement);
}

function idOf(value: unknown): string | undefined {
    return isModdleElement(value) && typeof value.id === 'string' ? value.id : undefined;
}

function nameOf(element: ModdleElement): string | undefined {
    return typeof element.name === 'string' ? element.name : undefined;
}

function boundsOf(bounds: ModdleElement | undefined): Bounds {
    return { x: Number(bounds?.x ?? 0), y: Number(bounds?.y ?? 0), width: Number(bounds?.width ?? 0), height: Number(bounds?.height ?? 0) };
}

function labelOf(di: ModdleElement): Bounds | undefined {
    const bounds = (di.label as ModdleElement | undefined)?.bounds as ModdleElement | undefined;
    return bounds ? boundsOf(bounds) : undefined;
}

function hasExpandedState(element: ModdleElement): boolean {
    return element.$instanceOf('bpmn:SubProcess') || element.$instanceOf('bpmn:CallActivity') || element.$instanceOf('bpmn:Participant');
}

/** As bpmn-js decides it: a subprocess is expanded when its shape says so, a participant when it has a process. */
function isExpanded(element: ModdleElement, di: ModdleElement): boolean {
    if (element.$instanceOf('bpmn:CallActivity')) return false;
    if (element.$instanceOf('bpmn:SubProcess')) return Boolean(di.isExpanded);
    if (element.$instanceOf('bpmn:Participant')) return Boolean(element.processRef);
    return true;
}

/**
 * Where an element sits on a plane: the nearest shape on the plane that contains it, and the outermost collapsed
 * subprocess on the plane that hides it. A flow element is contained by its subprocess, and a process by the
 * participant that references it.
 */
function placement(element: ModdleElement, shapesOnPlane: Map<string, ModdleElement>): { parentId?: string; hiddenBy?: string } {
    let parentId: string | undefined;
    let hiddenBy: string | undefined;
    for (let container = containerOf(element, shapesOnPlane); container; container = containerOf(container, shapesOnPlane)) {
        const id = idOf(container);
        const di = id === undefined ? undefined : shapesOnPlane.get(id);
        if (!id || !di) continue;
        parentId ??= id;
        if (container.$instanceOf('bpmn:SubProcess') && !isExpanded(container, di)) hiddenBy = id;
    }
    return { parentId, hiddenBy };
}

function containerOf(element: ModdleElement, shapesOnPlane: Map<string, ModdleElement>): ModdleElement | undefined {
    if (element.$instanceOf('bpmn:Process')) {
        // The participant drawn on this plane that the process runs in.
        for (const di of shapesOnPlane.values()) {
            const participant = di.bpmnElement as ModdleElement;
            if (participant.$instanceOf('bpmn:Participant') && participant.processRef === element) return participant;
        }
        return undefined;
    }
    if (element.$instanceOf('bpmn:Participant') || element.$instanceOf('bpmn:Definitions')) return undefined;
    const parent = element.$parent;
    return isModdleElement(parent) ? parent : undefined;
}

/**
 * The element an edge leaves or enters. A data association is drawn between its data and the activity that owns it,
 * as bpmn-js draws it.
 */
function endOf(element: ModdleElement, end: 'source' | 'target'): string | undefined {
    const owner = isModdleElement(element.$parent) ? element.$parent : undefined;
    if (element.$instanceOf('bpmn:DataInputAssociation') && end === 'target') return idOf(owner);
    if (element.$instanceOf('bpmn:DataOutputAssociation') && end === 'source') return idOf(owner);
    const reference = element[`${end}Ref`];
    return idOf(Array.isArray(reference) ? reference[0] : reference);
}

function withoutUndefined<T extends object>(value: T): T {
    return Object.fromEntries(Object.entries(value).filter(([, entry]) => entry !== undefined)) as T;
}
