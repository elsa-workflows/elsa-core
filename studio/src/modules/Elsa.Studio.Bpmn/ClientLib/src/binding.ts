/**
 * Reading and writing an element's `elsa:activityBinding`, on the moddle objects bpmn-js and bpmn-moddle produce.
 *
 * Everything here is pure: it reads an element, or computes the element's new extension values, and never changes the
 * element. A designer applies the result through its own undoable command, so that `setBinding` is one undo step.
 */
import type { BpmnActivityBinding } from './contract.js';
import { BINDING_TYPE, INPUT_TYPE } from './elsa-moddle.js';

/** The part of a moddle element these rules read. */
export interface ModdleElement {
    readonly $type: string;
    $instanceOf(type: string): boolean;
    [property: string]: unknown;
}

/** The part of a moddle instance these rules use to create elements. */
export interface ModdleFactory {
    create(type: string, attributes?: Record<string, unknown>): ModdleElement;
}

/**
 * Whether Elsa's import would bind this element to an activity: any task, except a send or receive task that names a
 * message, which Elsa runs as publishing or waiting for that message.
 */
export function isBindable(element: ModdleElement): boolean {
    if (!element.$instanceOf('bpmn:Task')) return false;
    const isMessageTask = element.$instanceOf('bpmn:SendTask') || element.$instanceOf('bpmn:ReceiveTask');
    return !(isMessageTask && element.messageRef);
}

/** The element's `elsa:activityBinding`, or `null` when it has none. Only the first one counts, as in Elsa's import. */
export function readBinding(element: ModdleElement): BpmnActivityBinding | null {
    const binding = extensionValues(element).find(value => value.$instanceOf(BINDING_TYPE));
    if (!binding) return null;
    const inputs = (binding.inputs as ModdleElement[] | undefined) ?? [];
    return {
        activityType: String(binding.activityType ?? ''),
        inputs: inputs.map(input => ({ name: String(input.name ?? ''), value: String(input.value ?? '') }))
    };
}

/**
 * The element's extension values with its binding replaced by `binding`, or removed for `null`. An existing binding is
 * replaced where it stood, so the element's other extension content keeps its place; a new one is added last.
 *
 * @throws TypeError when `binding` names no activity type, or names an input twice; Elsa's import refuses both.
 */
export function extensionValuesWithBinding(moddle: ModdleFactory, element: ModdleElement, binding: BpmnActivityBinding | null): ModdleElement[] {
    const values = extensionValues(element);
    const replacement = binding === null ? null : createBindingElement(moddle, binding);
    const index = values.findIndex(value => value.$instanceOf(BINDING_TYPE));

    if (index < 0) return replacement ? [...values, replacement] : [...values];

    const rest = values.filter((value, position) => position !== index && !value.$instanceOf(BINDING_TYPE));
    if (!replacement) return rest;
    return [...rest.slice(0, index), replacement, ...rest.slice(index)];
}

/**
 * A new `elsa:activityBinding` element for `binding`.
 *
 * @throws TypeError when `binding` names no activity type, or names an input twice.
 */
export function createBindingElement(moddle: ModdleFactory, binding: BpmnActivityBinding): ModdleElement {
    if (!binding.activityType) throw new TypeError('An activity binding needs an activity type.');

    const names = new Set<string>();
    for (const input of binding.inputs) {
        if (names.has(input.name)) throw new TypeError(`The '${binding.activityType}' binding names the input '${input.name}' twice.`);
        names.add(input.name);
    }

    const inputs = binding.inputs.map(input => moddle.create(INPUT_TYPE, { name: input.name, value: input.value }));
    return moddle.create(BINDING_TYPE, { activityType: binding.activityType, inputs });
}

function extensionValues(element: ModdleElement): ModdleElement[] {
    const extensionElements = element.extensionElements as ModdleElement | undefined;
    return ((extensionElements?.values as ModdleElement[] | undefined) ?? []);
}
