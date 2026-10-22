/**
 * Computes a designer's load warnings by comparing the document it was given with what a save right after loading
 * produces. A parser's own messages are not enough: content it accepts can still be lost on save.
 *
 * The comparison is semantic. Elements and attributes are compared by namespace URI and local name, so prefixes,
 * attribute order and whitespace-only text do not matter. Elements are matched by `id` (or `name`) where they have one,
 * else by position among same-named siblings. Content the save adds is not reported. Only content of the original that
 * the save loses (`dropped`) or writes differently (`rewritten`) is.
 *
 * Diagram layout (DI) follows its own rule: a shape or edge is matched by the element it draws, and keeps its layout
 * when its bounds, waypoints and label bounds are equal, whichever diagram plane holds it. A move to another plane is
 * reported as `rewritten`.
 *
 * Needs a global `DOMParser`, as every browser has.
 */
import type { BpmnLoadWarning } from './contract.js';

const XMLNS = 'http://www.w3.org/2000/xmlns/';
const XSI = 'http://www.w3.org/2001/XMLSchema-instance';
export const BPMN_NAMESPACE_URI = 'http://www.omg.org/spec/BPMN/20100524/MODEL';
const BPMNDI = 'http://www.omg.org/spec/BPMN/20100524/DI';
const DC = 'http://www.omg.org/spec/DD/20100524/DC';
const DI = 'http://www.omg.org/spec/DD/20100524/DI';

/**
 * Attributes of the BPMN namespace whose schema default a writer may leave out. Omitting one changes nothing, so it is
 * not reported. Matches the defaults in bpmn-moddle's BPMN schema (__tests__/load-warnings.test.ts checks this).
 */
export const BPMN_ATTRIBUTE_DEFAULTS: Readonly<Record<string, string>> = {
    behavior: 'All',
    cancelActivity: 'true',
    cancelRemainingInstances: 'true',
    completionQuantity: '1',
    eventGatewayType: 'Exclusive',
    expressionLanguage: 'http://www.w3.org/1999/XPath',
    gatewayDirection: 'Unspecified',
    instantiate: 'false',
    isCollection: 'false',
    isForCompensation: 'false',
    isInterrupting: 'true',
    isReference: 'false',
    isSequential: 'false',
    isUnlimited: 'true',
    loopType: 'None',
    maximum: '1',
    minimum: '0',
    mustUnderstand: 'false',
    parallelMultiple: 'false',
    startQuantity: '1',
    testBefore: 'false',
    textFormat: 'text/plain',
    triggeredByEvent: 'false',
    typeLanguage: 'http://www.w3.org/2001/XMLSchema',
    waitForCompletion: 'true'
};

interface CanonicalElement {
    /** Namespace URI and local name, as `{uri}local`. */
    readonly key: string;
    /** The name as the original document wrote it, for messages. */
    readonly name: string;
    readonly attributes: Map<string, { name: string; value: string }>;
    readonly children: CanonicalElement[];
    readonly text: string;
}

/** Compares `original` with `saved` and reports what the save loses or rewrites. */
export function computeLoadWarnings(original: string, saved: string): BpmnLoadWarning[] {
    const before = parseXml(original);
    const after = parseXml(saved);
    const warnings: BpmnLoadWarning[] = [];

    compareElements(canonical(before.documentElement), canonical(after.documentElement), null, warnings);
    compareLayout(before, after, warnings);
    compareLexicalContent(before, after, warnings);
    return warnings;
}

/** Parses `xml`, and throws when it is not well-formed. */
export function parseXml(xml: string): Document {
    const document = new DOMParser().parseFromString(xml, 'application/xml');
    const error = document.getElementsByTagName('parsererror')[0];
    if (error) throw new Error(`The document is not well-formed XML: ${error.textContent?.trim()}`);
    return document;
}

function qualified(node: { namespaceURI: string | null; localName: string }): string {
    return node.namespaceURI ? `{${node.namespaceURI}}${node.localName}` : node.localName;
}

function canonical(element: Element): CanonicalElement {
    const attributes = new Map<string, { name: string; value: string }>();
    for (const attribute of Array.from(element.attributes)) {
        if (attribute.namespaceURI === XMLNS) continue;
        const isType = attribute.namespaceURI === XSI && attribute.localName === 'type';
        attributes.set(qualified(attribute), { name: attribute.name, value: isType ? resolveQName(element, attribute.value) : attribute.value });
    }

    const children: CanonicalElement[] = [];
    let text = '';
    for (const child of Array.from(element.childNodes)) {
        if (child.nodeType === 1) {
            const childElement = child as Element;
            // DI is compared by the element it draws, not by its place in the tree: see compareLayout.
            if (childElement.namespaceURI === BPMNDI && childElement.localName === 'BPMNDiagram') continue;
            children.push(canonical(childElement));
        } else if (child.nodeType === 3 || child.nodeType === 4) {
            text += child.nodeValue ?? '';
        }
    }

    return { key: qualified(element), name: element.nodeName, attributes, children, text: text.trim() };
}

/** Resolves a prefixed QName value (used for `xsi:type`) to `{uri}local`, so a renamed prefix is not a difference. */
function resolveQName(element: Element, value: string): string {
    const match = /^([\w.-]+):([\w.-]+)$/.exec(value);
    const uri = match ? element.lookupNamespaceURI(match[1]) : null;
    return match && uri ? `{${uri}}${match[2]}` : value;
}

function identityOf(node: CanonicalElement): string | undefined {
    return node.attributes.get('id')?.value ?? node.attributes.get('name')?.value;
}

/** Groups children by what matches them across the two documents, keeping document order within each group. */
function groupChildren(node: CanonicalElement): Map<string, CanonicalElement[]> {
    const groups = new Map<string, CanonicalElement[]>();
    for (const child of node.children) {
        const identity = identityOf(child);
        const key = identity === undefined ? child.key : `${child.key}#${identity}`;
        const group = groups.get(key);
        if (group) group.push(child);
        else groups.set(key, [child]);
    }
    return groups;
}

function compareElements(before: CanonicalElement, after: CanonicalElement, ownerId: string | null, warnings: BpmnLoadWarning[]): void {
    const id = before.attributes.get('id')?.value ?? ownerId;
    const subject = describe(before, ownerId);

    for (const [key, attribute] of before.attributes) {
        const kept = after.attributes.get(key);
        if (kept === undefined) {
            if (!isOmittedDefault(before.key, key, attribute.value)) warnings.push(warning('dropped', `The attribute '${attribute.name}' of ${subject} is lost.`, id));
        } else if (kept.value !== attribute.value) {
            warnings.push(warning('rewritten', `The attribute '${attribute.name}' of ${subject} changes from '${clip(attribute.value)}' to '${clip(kept.value)}'.`, id));
        }
    }

    if (before.text && !after.text) warnings.push(warning('dropped', `The text of ${subject} is lost.`, id));
    else if (before.text && after.text !== before.text) warnings.push(warning('rewritten', `The text of ${subject} changes.`, id));

    const keptGroups = groupChildren(after);
    for (const [key, group] of groupChildren(before)) {
        const kept = keptGroups.get(key) ?? [];
        group.forEach((child, index) => {
            const match = kept[index];
            if (match) compareElements(child, match, id, warnings);
            else warnings.push(lost(child, id));
        });
    }
}

function lost(node: CanonicalElement, ownerId: string | null): BpmnLoadWarning {
    const id = node.attributes.get('id')?.value;
    return id
        ? warning('dropped', `'${id}' (${node.name}) is lost.`, id)
        : warning('dropped', `A ${node.name} element${ownerId ? ` inside '${ownerId}'` : ''} is lost.`, ownerId);
}

function describe(node: CanonicalElement, ownerId: string | null): string {
    const id = node.attributes.get('id')?.value;
    if (id) return `'${id}'`;
    return ownerId ? `a ${node.name} element inside '${ownerId}'` : `the ${node.name} element`;
}

function isOmittedDefault(elementKey: string, attributeKey: string, value: string): boolean {
    // Only a BPMN element's attributes have these defaults. They are unqualified, so their key is the bare local name.
    return elementKey.startsWith(`{${BPMN_NAMESPACE_URI}}`) && Object.hasOwn(BPMN_ATTRIBUTE_DEFAULTS, attributeKey) && BPMN_ATTRIBUTE_DEFAULTS[attributeKey] === value;
}

interface Layout {
    /** The element the shape or edge draws. */
    readonly elementId: string;
    /** The element whose diagram plane holds it. */
    readonly plane: string | null;
    /** Bounds, waypoints and label bounds, as numbers in document order. */
    readonly geometry: number[];
    readonly attributes: Map<string, { name: string; value: string }>;
}

function compareLayout(before: Document, after: Document, warnings: BpmnLoadWarning[]): void {
    const keptLayout = layoutOf(after);

    for (const [elementId, layout] of layoutOf(before)) {
        const kept = keptLayout.get(elementId);
        if (!kept) {
            warnings.push(warning('dropped', `The diagram layout of '${elementId}' is lost.`, elementId));
            continue;
        }

        const geometryChanges = kept.geometry.length !== layout.geometry.length || kept.geometry.some((value, index) => value !== layout.geometry[index]);
        if (kept.plane !== layout.plane) {
            const how = geometryChanges ? 'and its position or size changes' : 'unchanged';
            warnings.push(warning('rewritten', `The diagram layout of '${elementId}' moves to the diagram of '${kept.plane ?? '(none)'}', ${how}.`, elementId));
        } else if (geometryChanges) {
            warnings.push(warning('rewritten', `The diagram layout of '${elementId}' changes.`, elementId));
        }

        for (const [key, attribute] of layout.attributes) {
            const keptAttribute = kept.attributes.get(key);
            if (keptAttribute === undefined) warnings.push(warning('dropped', `The diagram attribute '${attribute.name}' of '${elementId}' is lost.`, elementId));
            else if (keptAttribute.value !== attribute.value) warnings.push(warning('rewritten', `The diagram attribute '${attribute.name}' of '${elementId}' changes.`, elementId));
        }
    }
}

function layoutOf(document: Document): Map<string, Layout> {
    const layouts = new Map<string, Layout>();
    for (const localName of ['BPMNShape', 'BPMNEdge']) {
        for (const element of Array.from(document.getElementsByTagNameNS(BPMNDI, localName))) {
            const elementId = element.getAttribute('bpmnElement');
            if (!elementId || layouts.has(elementId)) continue;

            const attributes = new Map<string, { name: string; value: string }>();
            for (const attribute of Array.from(element.attributes)) {
                // The shape's own id is not layout, and bpmn-js writes this edge default only when it differs.
                if (attribute.namespaceURI === XMLNS || attribute.name === 'id' || attribute.name === 'bpmnElement') continue;
                if (attribute.name === 'messageVisibleKind' && attribute.value === 'initiating') continue;
                attributes.set(qualified(attribute), { name: attribute.name, value: attribute.value });
            }

            layouts.set(elementId, { elementId, plane: planeOf(element), geometry: geometryOf(element), attributes });
        }
    }
    return layouts;
}

function planeOf(element: Element): string | null {
    for (let parent = element.parentElement; parent; parent = parent.parentElement) {
        if (parent.namespaceURI === BPMNDI && parent.localName === 'BPMNPlane') return parent.getAttribute('bpmnElement');
    }
    return null;
}

function geometryOf(element: Element): number[] {
    const numbers: number[] = [];
    const walk = (node: Element): void => {
        for (const child of Array.from(node.children)) {
            if (child.namespaceURI === DC && child.localName === 'Bounds') {
                for (const name of ['x', 'y', 'width', 'height']) numbers.push(Number(child.getAttribute(name) ?? 0));
            } else if (child.namespaceURI === DI && child.localName === 'waypoint') {
                for (const name of ['x', 'y']) numbers.push(Number(child.getAttribute(name) ?? 0));
            } else {
                walk(child);
            }
        }
    };
    walk(element);
    return numbers;
}

function compareLexicalContent(before: Document, after: Document, warnings: BpmnLoadWarning[]): void {
    const lostComments = count(before, 128) - count(after, 128);
    if (lostComments > 0) warnings.push(warning('dropped', lostComments === 1 ? 'A comment is lost.' : `${lostComments} comments are lost.`));
}

function count(document: Document, whatToShow: number): number {
    const walker = document.createTreeWalker(document, whatToShow);
    let total = 0;
    while (walker.nextNode()) total++;
    return total;
}

function warning(kind: BpmnLoadWarning['kind'], message: string, elementId?: string | null): BpmnLoadWarning {
    return elementId ? { kind, message, elementId } : { kind, message };
}

function clip(value: string): string {
    return value.length > 60 ? `${value.slice(0, 57)}...` : value;
}

/** True when the document draws nothing: no shape or edge in any diagram. Such a document needs generated layout. */
export function lacksDiagramLayout(xml: string): boolean {
    const document = parseXml(xml);
    return document.getElementsByTagNameNS(BPMNDI, 'BPMNShape').length === 0 && document.getElementsByTagNameNS(BPMNDI, 'BPMNEdge').length === 0;
}
