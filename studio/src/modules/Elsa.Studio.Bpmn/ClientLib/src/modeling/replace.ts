/**
 * Replacing an element with one of another type, as the bpmn-js designer's replace menu does: a task with another kind
 * of task, a gateway with another gateway, an event with another event or event definition. The element keeps its id,
 * its shape and everything both types have, such as its name, its binding and its flows.
 */
import type { ModdleElement } from '../binding.js';
import type { Command } from './command-stack.js';
import { deleteElements } from './delete.js';
import type { BpmnDocument } from './document.js';

/** What an element is replaced with: its type and, for an event, its event definition and how it interrupts. */
export interface ReplaceTarget {
    type: string;
    eventDefinitionType?: string;
    /** For a boundary event. */
    cancelActivity?: boolean;
    /** For a start event. */
    isInterrupting?: boolean;
}

/** An entry of the replace menu, as the bpmn-js designer labels it. */
export interface ReplaceOption {
    label: string;
    target: ReplaceTarget;
}

const option = (label: string, type: string, extra: Omit<ReplaceTarget, 'type'> = {}): ReplaceOption => ({ label, target: { type, ...extra } });
const definition = (kind: string) => `bpmn:${kind}EventDefinition`;

const TASKS = [
    option('Task', 'bpmn:Task'),
    option('User task', 'bpmn:UserTask'),
    option('Service task', 'bpmn:ServiceTask'),
    option('Send task', 'bpmn:SendTask'),
    option('Receive task', 'bpmn:ReceiveTask'),
    option('Manual task', 'bpmn:ManualTask'),
    option('Business rule task', 'bpmn:BusinessRuleTask'),
    option('Script task', 'bpmn:ScriptTask')
];

const GATEWAYS = [
    option('Exclusive gateway', 'bpmn:ExclusiveGateway'),
    option('Parallel gateway', 'bpmn:ParallelGateway'),
    option('Inclusive gateway', 'bpmn:InclusiveGateway'),
    option('Complex gateway', 'bpmn:ComplexGateway'),
    option('Event-based gateway', 'bpmn:EventBasedGateway')
];

const NONE_EVENTS = [
    option('Start event', 'bpmn:StartEvent'),
    option('Intermediate throw event', 'bpmn:IntermediateThrowEvent'),
    option('End event', 'bpmn:EndEvent')
];

const START_EVENTS = [
    ...NONE_EVENTS,
    option('Message start event', 'bpmn:StartEvent', { eventDefinitionType: definition('Message'), isInterrupting: true }),
    option('Timer start event', 'bpmn:StartEvent', { eventDefinitionType: definition('Timer'), isInterrupting: true }),
    option('Conditional start event', 'bpmn:StartEvent', { eventDefinitionType: definition('Conditional'), isInterrupting: true }),
    option('Signal start event', 'bpmn:StartEvent', { eventDefinitionType: definition('Signal'), isInterrupting: true })
];

const INTERMEDIATE_EVENTS = [
    ...NONE_EVENTS,
    option('Message intermediate catch event', 'bpmn:IntermediateCatchEvent', { eventDefinitionType: definition('Message') }),
    option('Message intermediate throw event', 'bpmn:IntermediateThrowEvent', { eventDefinitionType: definition('Message') }),
    option('Timer intermediate catch event', 'bpmn:IntermediateCatchEvent', { eventDefinitionType: definition('Timer') }),
    option('Escalation intermediate throw event', 'bpmn:IntermediateThrowEvent', { eventDefinitionType: definition('Escalation') }),
    option('Conditional intermediate catch event', 'bpmn:IntermediateCatchEvent', { eventDefinitionType: definition('Conditional') }),
    option('Link intermediate catch event', 'bpmn:IntermediateCatchEvent', { eventDefinitionType: definition('Link') }),
    option('Link intermediate throw event', 'bpmn:IntermediateThrowEvent', { eventDefinitionType: definition('Link') }),
    option('Compensation intermediate throw event', 'bpmn:IntermediateThrowEvent', { eventDefinitionType: definition('Compensate') }),
    option('Signal intermediate catch event', 'bpmn:IntermediateCatchEvent', { eventDefinitionType: definition('Signal') }),
    option('Signal intermediate throw event', 'bpmn:IntermediateThrowEvent', { eventDefinitionType: definition('Signal') })
];

const END_EVENTS = [
    ...NONE_EVENTS,
    option('Message end event', 'bpmn:EndEvent', { eventDefinitionType: definition('Message') }),
    option('Escalation end event', 'bpmn:EndEvent', { eventDefinitionType: definition('Escalation') }),
    option('Error end event', 'bpmn:EndEvent', { eventDefinitionType: definition('Error') }),
    option('Cancel end event', 'bpmn:EndEvent', { eventDefinitionType: definition('Cancel') }),
    option('Compensation end event', 'bpmn:EndEvent', { eventDefinitionType: definition('Compensate') }),
    option('Signal end event', 'bpmn:EndEvent', { eventDefinitionType: definition('Signal') }),
    option('Terminate end event', 'bpmn:EndEvent', { eventDefinitionType: definition('Terminate') })
];

const boundary = (label: string, kind: string, cancelActivity: boolean) =>
    option(label, 'bpmn:BoundaryEvent', { eventDefinitionType: definition(kind), cancelActivity });

const BOUNDARY_EVENTS = [
    boundary('Message boundary event', 'Message', true),
    boundary('Timer boundary event', 'Timer', true),
    boundary('Escalation boundary event', 'Escalation', true),
    boundary('Conditional boundary event', 'Conditional', true),
    boundary('Error boundary event', 'Error', true),
    boundary('Cancel boundary event', 'Cancel', true),
    boundary('Signal boundary event', 'Signal', true),
    boundary('Compensation boundary event', 'Compensate', true),
    boundary('Message boundary event (non-interrupting)', 'Message', false),
    boundary('Timer boundary event (non-interrupting)', 'Timer', false),
    boundary('Escalation boundary event (non-interrupting)', 'Escalation', false),
    boundary('Conditional boundary event (non-interrupting)', 'Conditional', false),
    boundary('Signal boundary event (non-interrupting)', 'Signal', false)
];

const TASK_TYPES = new Set(TASKS.map(entry => entry.target.type));

/**
 * The replace menu's entries for the element with `elementId`, as the bpmn-js designer offers them, without the one
 * the element already is. Empty for an element this kernel does not replace yet: subprocesses, call activities, event
 * subprocess start events, data references, participants and lanes.
 */
export function replaceOptions(document: BpmnDocument, elementId: string): ReplaceOption[] {
    const element = document.element(elementId);
    if (!element) return [];
    return candidatesFor(element).filter(entry => !isCurrent(element, entry.target)).map(entry => ({ label: entry.label, target: { ...entry.target } }));
}

/**
 * Replaces the element with `elementId` by one of `target`'s type, as the bpmn-js designer does:
 *
 * - it keeps the element's id, its place in its container, and its shapes on every plane;
 * - it keeps every property both types have, such as its name, documentation, binding, flows and loop characteristics,
 *   and drops the rest, such as a script task's script;
 * - an event keeps its event definitions when they are of the target's kind, and otherwise gets a new one of that kind,
 *   or none for a plain event;
 * - every reference to the element, such as a flow's source or target, a lane's `flowNodeRef` or a boundary event's
 *   `attachedToRef`, refers to the new one;
 * - a start event loses its incoming flows and an end event its outgoing flows, which they cannot have.
 *
 * The new element and any new event definition are made once, so a redo puts back the objects that later commands
 * refer to.
 *
 * @throws Error when the document has no element with `elementId`, or when `target` is not one of its replace options.
 */
export function replaceElement(document: BpmnDocument, elementId: string, target: ReplaceTarget): Command {
    const element = document.element(elementId);
    if (!element) throw new Error(`The document has no element '${elementId}'.`);
    const chosen = { ...target };
    const allowed = replaceOptions(document, elementId).some(entry => sameTarget(entry.target, chosen));
    if (!allowed) throw new Error(`The element '${elementId}' cannot be replaced with a ${describe(chosen)}.`);
    const dropped = flowsDroppedBy(element, chosen.type).map(flow => String(flow.id));

    const made: Made = {};
    let operations: Operation[] = [];
    let removal: Command | undefined;
    return {
        elementIds: [elementId, ...dropped],
        execute() {
            // The element found when the command was created: an earlier command in the same compound can have renamed it.
            if (!containerOf(element)) throw new Error(`The element '${elementId}' is no longer in the document.`);
            operations = planReplace(document, element, chosen, made);
            for (const operation of operations) operation.apply();
            document.reindex();
            const replacement = document.element(String(element.id))!;
            const invalid = flowsDroppedBy(replacement, chosen.type).map(flow => String(flow.id));
            removal = invalid.length > 0 ? deleteElements(document, invalid) : undefined;
            removal?.execute();
        },
        revert() {
            removal?.revert();
            for (const operation of [...operations].reverse()) operation.revert();
            document.reindex();
        }
    };
}

interface Operation {
    apply(): void;
    revert(): void;
}

/** The objects a replace makes, kept from its first execute for the ones after it. */
interface Made {
    replacement?: ModdleElement;
    definition?: ModdleElement;
}

function candidatesFor(element: ModdleElement): ReplaceOption[] {
    const parent = element.$parent as ModdleElement | undefined;
    const inTransaction = (candidate: ModdleElement | undefined) => candidate?.$instanceOf('bpmn:Transaction') === true;
    if (TASK_TYPES.has(element.$type)) return TASKS;
    if (element.$instanceOf('bpmn:Gateway')) return GATEWAYS;
    if (element.$instanceOf('bpmn:BoundaryEvent')) {
        return BOUNDARY_EVENTS.filter(entry => entry.target.eventDefinitionType !== definition('Cancel') || inTransaction(element.attachedToRef as ModdleElement | undefined));
    }
    if (element.$instanceOf('bpmn:StartEvent')) {
        // Event subprocesses come in step 7; a subprocess's start event is a plain one, as in the bpmn-js designer.
        if (parent?.$instanceOf('bpmn:SubProcess')) return parent.triggeredByEvent ? [] : NONE_EVENTS;
        return START_EVENTS;
    }
    if (element.$instanceOf('bpmn:IntermediateThrowEvent') || element.$instanceOf('bpmn:IntermediateCatchEvent')) return INTERMEDIATE_EVENTS;
    if (element.$instanceOf('bpmn:EndEvent')) {
        return END_EVENTS.filter(entry => entry.target.eventDefinitionType !== definition('Cancel') || inTransaction(parent));
    }
    return [];
}

/** Whether `target` describes what `element` already is. */
function isCurrent(element: ModdleElement, target: ReplaceTarget): boolean {
    if (element.$type !== target.type) return false;
    if (definitionTypeOf(element) !== target.eventDefinitionType) return false;
    if (target.cancelActivity !== undefined && (element.cancelActivity !== false) !== target.cancelActivity) return false;
    if (target.isInterrupting !== undefined && (element.isInterrupting !== false) !== target.isInterrupting) return false;
    return true;
}

function sameTarget(left: ReplaceTarget, right: ReplaceTarget): boolean {
    return left.type === right.type && left.eventDefinitionType === right.eventDefinitionType
        && left.cancelActivity === right.cancelActivity && left.isInterrupting === right.isInterrupting;
}

function describe(target: ReplaceTarget): string {
    return target.eventDefinitionType ? `${target.type} with a ${target.eventDefinitionType}` : target.type;
}

function definitionTypeOf(element: ModdleElement): string | undefined {
    const definitions = element.eventDefinitions as ModdleElement[] | undefined;
    return definitions?.[0]?.$type;
}

/** The flows an element of `type` cannot have: a start event's incoming flows and an end event's outgoing flows. */
function flowsDroppedBy(element: ModdleElement, type: string): ModdleElement[] {
    if (type === 'bpmn:StartEvent') return [...((element.incoming as ModdleElement[] | undefined) ?? [])];
    if (type === 'bpmn:EndEvent') return [...((element.outgoing as ModdleElement[] | undefined) ?? [])];
    return [];
}

function planReplace(document: BpmnDocument, element: ModdleElement, target: ReplaceTarget, made: Made): Operation[] {
    const { moddle } = document;
    const replacement = made.replacement ??= moddle.create(target.type, { id: element.id });
    const operations: Operation[] = [];

    // Everything both types declare and the element has set, except what the target decides.
    const targetProperties = new Set(descriptorOf(replacement).map(property => property.name));
    const decided = new Set(['id', 'eventDefinitions', 'cancelActivity', 'isInterrupting']);
    for (const property of descriptorOf(element)) {
        if (decided.has(property.name) || !targetProperties.has(property.name) || !Object.prototype.hasOwnProperty.call(element, property.name)) continue;
        const value = element[property.name];
        replacement[property.name] = Array.isArray(value) ? [...value] : value;
        if (!property.isReference) operations.push(...adopt(value, replacement));
    }
    Object.assign(replacement.$attrs as Record<string, unknown>, element.$attrs as Record<string, unknown>);
    // Only a value other than the default needs setting: bpmn-moddle does not write a default.
    if (target.cancelActivity === false) replacement.cancelActivity = false;
    if (target.isInterrupting === false) replacement.isInterrupting = false;

    if (target.eventDefinitionType) {
        const definitions = (element.eventDefinitions as ModdleElement[] | undefined) ?? [];
        if (definitions.some(candidate => candidate.$type === target.eventDefinitionType)) {
            replacement.eventDefinitions = [...definitions];
            operations.push(...adopt(definitions, replacement));
        } else {
            const created = made.definition ??= moddle.create(target.eventDefinitionType, { id: freshId(document, target.eventDefinitionType.replace(/^[^:]*:/, '')) });
            if (target.eventDefinitionType === 'bpmn:LinkEventDefinition') created.name = '';
            created.$parent = replacement;
            replacement.eventDefinitions = [created];
        }
    }

    const container = containerOf(element)!;
    operations.push(swap(container.owner, container.property, element, replacement));
    walk(document.definitions, owner => {
        for (const property of descriptorOf(owner)) {
            if (property.isReference) operations.push(...retarget(owner, property.name, element, replacement));
        }
    });

    // As the bpmn-js designer does, an exclusive gateway shows its marker.
    if (target.type === 'bpmn:ExclusiveGateway') {
        walk(document.definitions, di => {
            if (di.$instanceOf('bpmndi:BPMNShape') && di.bpmnElement === element && di.isMarkerVisible !== true) operations.push(setProperty(di, 'isMarkerVisible', true));
        });
    }
    return operations;
}

/** Makes `owner` the parent of the elements `value` holds, and gives them their parent back on revert. */
function adopt(value: unknown, owner: ModdleElement): Operation[] {
    return (Array.isArray(value) ? value : [value]).filter(isModdleElement).map(child => {
        const previous = child.$parent;
        return { apply: () => { child.$parent = owner; }, revert: () => { child.$parent = previous; } };
    });
}

/** Puts `replacement` where `element` is in the property `property` of `owner`. */
function swap(owner: ModdleElement, property: string, element: ModdleElement, replacement: ModdleElement): Operation {
    return {
        apply() {
            const value = owner[property];
            if (Array.isArray(value)) value[value.indexOf(element)] = replacement;
            else owner[property] = replacement;
            replacement.$parent = owner;
        },
        revert() {
            const value = owner[property];
            if (Array.isArray(value)) value[value.indexOf(replacement)] = element;
            else owner[property] = element;
        }
    };
}

/** Makes the reference `name` of `owner` refer to `replacement` wherever it referred to `element`. */
function retarget(owner: ModdleElement, name: string, element: ModdleElement, replacement: ModdleElement): Operation[] {
    const value = owner[name];
    if (Array.isArray(value)) {
        return value.flatMap((entry, index) => entry === element ? [{
            apply: () => { (owner[name] as unknown[])[index] = replacement; },
            revert: () => { (owner[name] as unknown[])[index] = element; }
        }] : []);
    }
    return value === element ? [{ apply: () => { owner[name] = replacement; }, revert: () => { owner[name] = element; } }] : [];
}

function setProperty(owner: ModdleElement, name: string, value: unknown): Operation {
    const had = Object.prototype.hasOwnProperty.call(owner, name);
    const previous = owner[name];
    return {
        apply: () => (owner.set as (name: string, value: unknown) => void)(name, value),
        revert: () => (owner.set as (name: string, value: unknown) => void)(name, had ? previous : undefined)
    };
}

/** The element's container and the property of it that holds the element, or `undefined` when it is not in the tree. */
function containerOf(element: ModdleElement): { owner: ModdleElement; property: string } | undefined {
    const owner = element.$parent as ModdleElement | undefined;
    if (!owner) return undefined;
    const property = descriptorOf(owner).find(candidate => {
        if (candidate.isReference) return false;
        const value = owner[candidate.name];
        return value === element || (Array.isArray(value) && value.includes(element));
    });
    return property ? { owner, property: property.name } : undefined;
}

/** `prefix_` and seven random base-36 characters, as bpmn-js generates ids, that no element has. */
function freshId(document: BpmnDocument, prefix: string): string {
    for (;;) {
        const id = `${prefix}_${Math.floor(Math.random() * 36 ** 7).toString(36).padStart(7, '0')}`;
        if (!document.hasId(id)) return id;
    }
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

function isModdleElement(value: unknown): value is ModdleElement {
    return typeof value === 'object' && value !== null && typeof (value as ModdleElement).$instanceOf === 'function';
}
