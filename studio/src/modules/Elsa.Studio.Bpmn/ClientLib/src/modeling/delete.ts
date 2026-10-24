/**
 * Deleting elements with what depends on them, as the bpmn-js designer does: what they contain, the boundary events
 * attached to them, the flows and associations that connect them, their drawings on every plane, and every reference
 * the rest of the document holds to any of it.
 */
import type { ModdleElement } from '../binding.js';
import type { Command } from './command-stack.js';
import type { BpmnDocument } from './document.js';

/** What can be deleted here. Participants and lanes are deleted with their lanes, which a later step adds. */
const DELETABLE = ['bpmn:FlowElement', 'bpmn:Artifact', 'bpmn:MessageFlow', 'bpmn:DataAssociation'];

/** Elements that connect two others, and go when either end goes. */
const CONNECTIONS = ['bpmn:SequenceFlow', 'bpmn:MessageFlow', 'bpmn:Association', 'bpmn:DataAssociation'];

/**
 * Deletes the elements with `elementIds`. It also deletes:
 *
 * - what they contain;
 * - the boundary events attached to anything deleted;
 * - the sequence flows, message flows, associations and data associations with a deleted end;
 * - a data object whose last reference is deleted;
 * - the shapes and edges that draw any of it, and the diagram of a deleted subprocess's own plane.
 *
 * References the rest of the document holds to what is deleted are removed: a flow node's `incoming` and `outgoing`, a
 * gateway's `default`, a lane's `flowNodeRef`, and any other. Undo puts every element and reference back where it was.
 * The elements are those with `elementIds` when the command is created, even if they are renamed before it executes;
 * what depends on them is worked out when it executes.
 *
 * @throws Error when the document has no element with one of `elementIds`, or when one is not a flow element, an
 * artifact, a message flow or a data association.
 */
export function deleteElements(document: BpmnDocument, elementIds: readonly string[]): Command {
    if (elementIds.length === 0) throw new Error('A delete needs at least one element.');
    const elements = elementIds.map(id => {
        const element = document.element(id);
        if (!element) throw new Error(`The document has no element '${id}'.`);
        if (!DELETABLE.some(type => element.$instanceOf(type))) throw new Error(`The element '${id}' cannot be deleted here.`);
        return element;
    });
    const affected = [...removalSet(document, elements)].map(element => element.id).filter((id): id is string => typeof id === 'string');

    let operations: Operation[] = [];
    return {
        elementIds: [...new Set(affected)],
        execute() {
            // The elements found when the command was created, not their ids again: an earlier command in the same compound
            // can have renamed one.
            operations = planDelete(document, removalSet(document, elements));
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

/** The elements given, and everything that is deleted with them, each with everything it contains. */
function removalSet(document: BpmnDocument, elements: readonly ModdleElement[]): Set<ModdleElement> {
    const removed = new Set<ModdleElement>();
    const add = (element: ModdleElement) => walk(element, contained => removed.add(contained));
    elements.forEach(add);

    for (let grew = true; grew;) {
        grew = false;
        walk(document.definitions, element => {
            if (removed.has(element) || isDiagram(element)) return;
            const ends = CONNECTIONS.some(type => element.$instanceOf(type)) ? [...referencesOf(element, 'sourceRef'), ...referencesOf(element, 'targetRef')] : [];
            const attachedTo = element.$instanceOf('bpmn:BoundaryEvent') ? referencesOf(element, 'attachedToRef') : [];
            if ([...ends, ...attachedTo].some(end => removed.has(end))) {
                add(element);
                grew = true;
            }
        });
        // A data object goes with its last reference, as the bpmn-js designer deletes it.
        const references = new Map<ModdleElement, ModdleElement[]>();
        walk(document.definitions, element => {
            if (!element.$instanceOf('bpmn:DataObjectReference')) return;
            for (const dataObject of referencesOf(element, 'dataObjectRef')) references.set(dataObject, [...(references.get(dataObject) ?? []), element]);
        });
        for (const [dataObject, owners] of references) {
            if (!removed.has(dataObject) && owners.every(owner => removed.has(owner))) {
                add(dataObject);
                grew = true;
            }
        }
    }
    return removed;
}

function planDelete(document: BpmnDocument, removed: Set<ModdleElement>): Operation[] {
    const operations: Operation[] = [];

    // Each removed element whose container stays is taken out of it; what it contains goes with it.
    for (const element of removed) {
        const parent = element.$parent as ModdleElement | undefined;
        if (!parent || removed.has(parent)) continue;
        operations.push(...detach(parent, element));
    }

    // Drawings of what is removed, and the diagram of a removed subprocess's own plane.
    const diagrams = (document.definitions.diagrams as ModdleElement[] | undefined) ?? [];
    for (const diagram of diagrams) {
        const plane = diagram.plane as ModdleElement | undefined;
        if (plane && removed.has(plane.bpmnElement as ModdleElement)) {
            operations.push(...detach(document.definitions, diagram));
            continue;
        }
        for (const di of (plane?.planeElement as ModdleElement[] | undefined) ?? []) {
            if (removed.has(di.bpmnElement as ModdleElement)) operations.push(...detach(plane!, di));
        }
    }

    // References the rest of the document holds to what is removed.
    walk(document.definitions, element => {
        if (removed.has(element) || isDiagram(element)) return;
        for (const property of descriptorOf(element)) {
            if (!property.isReference) continue;
            for (const referenced of referencesOf(element, property.name)) {
                if (removed.has(referenced)) operations.push(...detach(element, referenced, property.name));
            }
        }
    });

    return operations;
}

/**
 * Takes `child` out of the property of `owner` that holds it: the reference property `name` when given, else the
 * containment property that holds it. The operation finds the child's position when it applies, and puts it back there.
 */
function detach(owner: ModdleElement, child: ModdleElement, name?: string): Operation[] {
    const property = name ?? descriptorOf(owner).find(candidate => !candidate.isReference && holds(owner[candidate.name], child))?.name;
    if (!property) return [];
    const value = owner[property];
    if (Array.isArray(value)) {
        let index = -1;
        return [{
            apply() {
                index = value.indexOf(child);
                if (index >= 0) value.splice(index, 1);
            },
            revert() {
                if (index >= 0) value.splice(index, 0, child);
            }
        }];
    }
    if (value !== child) return [];
    return [{
        apply: () => set(owner, property, undefined),
        revert: () => set(owner, property, child)
    }];
}

function holds(value: unknown, child: ModdleElement): boolean {
    return value === child || (Array.isArray(value) && value.includes(child));
}

/** The elements the property `name` of `element` refers to: none, one, or several. */
function referencesOf(element: ModdleElement, name: string): ModdleElement[] {
    const value = element[name];
    return (Array.isArray(value) ? value : [value]).filter(isModdleElement);
}

/** Calls `visit` for `element` and every element it contains, following containment and never references. */
function walk(element: ModdleElement, visit: (element: ModdleElement) => void): void {
    visit(element);
    for (const property of descriptorOf(element)) {
        if (property.isReference) continue;
        const value = element[property.name];
        for (const child of Array.isArray(value) ? value : [value]) {
            if (isModdleElement(child)) walk(child, visit);
        }
    }
}

function descriptorOf(element: ModdleElement): { name: string; isReference?: boolean }[] {
    return (element.$descriptor as { properties: { name: string; isReference?: boolean }[] } | undefined)?.properties ?? [];
}

function isDiagram(element: ModdleElement): boolean {
    return element.$type.startsWith('bpmndi:') || element.$type.startsWith('dc:') || element.$type.startsWith('di:');
}

function isModdleElement(value: unknown): value is ModdleElement {
    return typeof value === 'object' && value !== null && typeof (value as ModdleElement).$instanceOf === 'function';
}

/** bpmn-moddle's `set`, which removes a property set to `undefined`. */
function set(element: ModdleElement, name: string, value: unknown): void {
    (element.set as (name: string, value: unknown) => void)(name, value);
}
