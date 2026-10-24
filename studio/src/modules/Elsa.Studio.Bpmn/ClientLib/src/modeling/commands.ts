/**
 * The edits the modeling kernel makes, as undoable commands. Creating a command checks it and changes nothing; the
 * command stack executes it.
 */
import { extensionValuesWithBinding, type ModdleElement } from '../binding.js';
import type { BpmnActivityBinding } from '../contract.js';
import type { Command } from './command-stack.js';
import type { BpmnDocument } from './document.js';

/**
 * Sets properties of the element with `elementId`. A property set to `undefined` is removed, and a namespaced attribute
 * the BPMN schema does not declare, such as `vendor:origin`, is set or removed as that attribute.
 *
 * @throws Error when the document has no element with `elementId`, or when `properties` gives it an id another element has.
 */
export function updateProperties(document: BpmnDocument, elementId: string, properties: Record<string, unknown>): Command {
    const element = document.element(elementId);
    if (!element) throw new Error(`The document has no element '${elementId}'.`);

    const newId = properties.id;
    if (newId !== undefined && newId !== elementId) {
        if (typeof newId !== 'string' || newId === '') throw new Error(`An element id must be a non-empty string.`);
        if (document.hasId(newId)) throw new Error(`The document already has an element '${newId}'.`);
    }

    const elementIds = typeof newId === 'string' && newId !== elementId ? [elementId, newId] : [elementId];
    return propertyChange(document, element, properties, elementIds);
}

/**
 * Replaces the binding of `element` with `binding`, or removes it for `null`, as one command. An emptied
 * `extensionElements` is removed, unless it carries foreign attributes, which the element has to keep. Returns `null`
 * when there is nothing to change: no binding to remove and no extension content.
 *
 * @throws TypeError when `binding` names no activity type, or names an input twice.
 */
export function setBinding(document: BpmnDocument, element: ModdleElement, binding: BpmnActivityBinding | null): Command | null {
    const values = extensionValuesWithBinding(document.moddle, element, binding);
    const extensionElements = element.extensionElements as ModdleElement | undefined;
    const elementIds = [String(element.id)];

    if (extensionElements && (values.length > 0 || hasForeignAttributes(extensionElements))) {
        return propertyChange(document, extensionElements, { values }, elementIds);
    }
    if (extensionElements) return propertyChange(document, element, { extensionElements: undefined }, elementIds);
    if (values.length > 0) {
        return propertyChange(document, element, { extensionElements: document.moddle.create('bpmn:ExtensionElements', { values }) }, elementIds);
    }
    return null;
}

/** Sets `properties` on `target`, any moddle element of the document, and puts back exactly what was there on revert. */
function propertyChange(document: BpmnDocument, target: ModdleElement, properties: Record<string, unknown>, elementIds: string[]): Command {
    const names = Object.keys(properties);
    // Whether each property was set, and to what, so revert removes what execute added rather than setting undefined.
    // Taken when the command executes, so a command after another in a compound sees what the earlier one set.
    let before: { name: string; isSet: boolean; value: unknown }[] = [];

    return {
        elementIds,
        execute() {
            before = names.map(name => ({ name, isSet: isSet(target, name), value: get(target, name) }));
            for (const name of names) set(target, name, properties[name]);
            document.reindex();
        },
        revert() {
            for (const { name, isSet, value } of before) set(target, name, isSet ? value : undefined);
            document.reindex();
        }
    };
}

function isSet(element: ModdleElement, name: string): boolean {
    return Object.prototype.hasOwnProperty.call(element, name) || Object.prototype.hasOwnProperty.call(attributesOf(element), name);
}

function get(element: ModdleElement, name: string): unknown {
    return (element.get as (name: string) => unknown)(name);
}

/** bpmn-moddle's `set`, which removes a property or a foreign attribute set to `undefined`. */
function set(element: ModdleElement, name: string, value: unknown): void {
    (element.set as (name: string, value: unknown) => void)(name, value);
}

function attributesOf(element: ModdleElement): Record<string, unknown> {
    return (element.$attrs as Record<string, unknown> | undefined) ?? {};
}

function hasForeignAttributes(element: ModdleElement): boolean {
    return Object.keys(attributesOf(element)).some(name => !name.startsWith('xmlns'));
}
