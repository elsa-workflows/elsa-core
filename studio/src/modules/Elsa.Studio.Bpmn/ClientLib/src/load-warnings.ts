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

/** The same for attributes of the BPMN DI namespace, such as a shape's or an edge's. */
export const BPMNDI_ATTRIBUTE_DEFAULTS: Readonly<Record<string, string>> = {
    messageVisibleKind: 'initiating'
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

/**
 * The element as the comparison sees it. Diagrams are left out: compareLayout compares them separately. With
 * `skip`, the elements it matches are left out too: that is how compareLayout reads diagram content apart from
 * geometry, which it compares on its own. The ids of diagram interchange elements do not matter and are left out;
 * the ids of foreign content inside them are kept.
 */
function canonical(element: Element, skip?: (child: Element) => boolean): CanonicalElement {
    const attributes = new Map<string, { name: string; value: string }>();
    for (const attribute of Array.from(element.attributes)) {
        if (attribute.namespaceURI === XMLNS) continue;
        if (isInterchange(element) && attribute.namespaceURI === null && attribute.localName === 'id') continue;
        const isType = attribute.namespaceURI === XSI && attribute.localName === 'type';
        attributes.set(qualified(attribute), { name: attribute.name, value: isType ? resolveQName(element, attribute.value) : attribute.value });
    }

    const children: CanonicalElement[] = [];
    let text = '';
    for (const child of Array.from(element.childNodes)) {
        if (child.nodeType === 1) {
            const childElement = child as Element;
            // DI is compared by the element it draws, not by its place in the tree: see compareLayout.
            if (isBpmnDi(childElement, 'BPMNDiagram') || skip?.(childElement)) continue;
            children.push(canonical(childElement, skip));
        } else if (child.nodeType === 3 || child.nodeType === 4) {
            text += child.nodeValue ?? '';
        }
    }

    // Whitespace between elements is formatting; any other text is compared exactly, spaces included.
    return { key: qualified(element), name: element.nodeName, attributes, children, text: text.trim() === '' ? '' : text };
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

function compareElements(before: CanonicalElement, after: CanonicalElement, ownerId: string | null, warnings: BpmnLoadWarning[], subject = describe(before, ownerId)): void {
    const id = before.attributes.get('id')?.value ?? ownerId;

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
    // Only the BPMN and BPMN DI elements' attributes have these defaults. They are unqualified, so their key is the bare
    // local name.
    const defaults = elementKey.startsWith(`{${BPMN_NAMESPACE_URI}}`) ? BPMN_ATTRIBUTE_DEFAULTS
        : elementKey.startsWith(`{${BPMNDI}}`) ? BPMNDI_ATTRIBUTE_DEFAULTS
        : null;
    return defaults !== null && Object.hasOwn(defaults, attributeKey) && defaults[attributeKey] === value;
}

interface Layout {
    /** The element the shape or edge draws. */
    readonly elementId: string;
    /** The element whose diagram plane holds it. */
    readonly plane: string | null;
    /** Bounds, waypoints and label bounds, as numbers in document order. */
    readonly geometry: number[];
    /** Everything else the shape or edge holds: its attributes, its label and any foreign content. */
    readonly content: CanonicalElement;
}

function compareLayout(before: Document, after: Document, warnings: BpmnLoadWarning[]): void {
    const keptLayouts = layoutOf(after);

    for (const [elementId, layouts] of layoutOf(before)) {
        // An element can be drawn more than once, for example in two diagrams. Each drawing is matched to one the save
        // keeps on the same plane, else to any it keeps; one left without a match is lost.
        const pairs = pair(layouts, keptLayouts.get(elementId) ?? [], (layout, kept) => kept.plane === layout.plane);
        layouts.forEach((layout, index) => {
            const kept = pairs[index];
            if (kept) compareDrawing(layout, kept, warnings);
            else warnings.push(warning('dropped', `${layouts.length > 1 ? 'A' : 'The'} diagram layout of '${elementId}' is lost.`, elementId));
        });
    }

    // What diagrams and planes hold besides shapes and edges, such as label styles and foreign content. A diagram is
    // matched by the element its plane draws, so one that moves keeps its match. Of several diagrams of the same
    // element, one is matched to an identical one the save keeps, else to one that has content, else to any. A diagram
    // with no content of its own has nothing to lose, so it is not matched at all.
    const keptDiagrams = diagramsOf(after);
    for (const [elementId, diagrams] of diagramsOf(before)) {
        const withContent = diagrams.filter(hasOwnContent);
        const pairs = pair(withContent, keptDiagrams.get(elementId) ?? [],
            (diagram, kept) => fingerprint(kept) === fingerprint(diagram),
            (_, kept) => hasOwnContent(kept));
        withContent.forEach((diagram, index) => {
            const kept = pairs[index];
            if (kept) compareElements(diagram, kept, elementId, warnings, `the diagram of '${elementId}'`);
            else warnings.push(warning('dropped', `${diagrams.length > 1 ? 'A' : 'The'} diagram of '${elementId}' is lost.`, elementId));
        });
    }
}

/**
 * Pairs each original with a kept item: in a pass per preference, with the first one left that the preference
 * accepts, and in a last pass with the first one left. Returns each original's match by index; undefined when none
 * is left.
 */
function pair<T>(originals: T[], kept: T[], ...preferences: ((original: T, kept: T) => boolean)[]): (T | undefined)[] {
    const unmatched = [...kept];
    const pairs: (T | undefined)[] = originals.map(() => undefined);
    for (const accepts of [...preferences, () => true]) {
        originals.forEach((original, index) => {
            if (pairs[index]) return;
            const match = unmatched.findIndex(candidate => accepts(original, candidate));
            if (match >= 0) pairs[index] = unmatched.splice(match, 1)[0];
        });
    }
    return pairs;
}

/** The element as one string. Two elements have the same fingerprint when they are the same, apart from attribute order. */
function fingerprint(node: CanonicalElement): string {
    const attributes = [...node.attributes].map(([key, attribute]) => [key, attribute.value]).sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0));
    return JSON.stringify([node.key, attributes, node.children.map(fingerprint), node.text]);
}

function compareDrawing(layout: Layout, kept: Layout, warnings: BpmnLoadWarning[]): void {
    const elementId = layout.elementId;
    const geometryChanges = kept.geometry.length !== layout.geometry.length || kept.geometry.some((value, index) => value !== layout.geometry[index]);
    if (kept.plane !== layout.plane) {
        const how = geometryChanges ? 'and its position or size changes' : 'unchanged';
        warnings.push(warning('rewritten', `The diagram layout of '${elementId}' moves to the diagram of '${kept.plane ?? '(none)'}', ${how}.`, elementId));
    } else if (geometryChanges) {
        warnings.push(warning('rewritten', `The diagram layout of '${elementId}' changes.`, elementId));
    }

    compareElements(layout.content, kept.content, elementId, warnings, `the diagram layout of '${elementId}'`);
}

/** Whether a diagram holds anything besides a plane naming the element it draws: a name, a label style, foreign content. */
function hasOwnContent(diagram: CanonicalElement): boolean {
    const plane = `{${BPMNDI}}BPMNPlane`;
    return diagram.attributes.size > 0 || diagram.children.some(child =>
        child.key !== plane || child.children.length > 0 || [...child.attributes.keys()].some(key => key !== 'bpmnElement'));
}

const isGeometry = (element: Element): boolean =>
    (element.namespaceURI === DC && element.localName === 'Bounds') || (element.namespaceURI === DI && element.localName === 'waypoint');

const isDrawing = (element: Element): boolean => isBpmnDi(element, 'BPMNShape') || isBpmnDi(element, 'BPMNEdge');

const isInterchange = (element: Element): boolean => element.namespaceURI === BPMNDI || element.namespaceURI === DC || element.namespaceURI === DI;

function isBpmnDi(element: Element, localName: string): boolean {
    return element.namespaceURI === BPMNDI && element.localName === localName;
}

/** Each diagram's own content, without its shapes and edges, keyed by the element its plane draws. */
function diagramsOf(document: Document): Map<string, CanonicalElement[]> {
    const diagrams = new Map<string, CanonicalElement[]>();
    for (const element of Array.from(document.getElementsByTagNameNS(BPMNDI, 'BPMNDiagram'))) {
        const plane = Array.from(element.children).find(child => isBpmnDi(child, 'BPMNPlane'));
        const elementId = plane?.getAttribute('bpmnElement');
        if (elementId) diagrams.set(elementId, [...(diagrams.get(elementId) ?? []), canonical(element, isDrawing)]);
    }
    return diagrams;
}

/** Every shape and edge, grouped by the element it draws, in document order. */
function layoutOf(document: Document): Map<string, Layout[]> {
    const layouts = new Map<string, Layout[]>();
    for (const localName of ['BPMNShape', 'BPMNEdge']) {
        for (const element of Array.from(document.getElementsByTagNameNS(BPMNDI, localName))) {
            const elementId = element.getAttribute('bpmnElement');
            if (!elementId) continue;
            const layout = { elementId, plane: planeOf(element), geometry: geometryOf(element), content: canonical(element, isGeometry) };
            layouts.set(elementId, [...(layouts.get(elementId) ?? []), layout]);
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
    // Comments are matched by their text, so one the save replaces with another still counts as lost.
    const kept = commentsOf(after);
    let lostComments = 0;
    for (const [text, total] of commentsOf(before)) {
        lostComments += Math.max(0, total - (kept.get(text) ?? 0));
    }
    if (lostComments > 0) warnings.push(warning('dropped', lostComments === 1 ? 'A comment is lost.' : `${lostComments} comments are lost.`));
}

/** How many times each comment text occurs in the document. */
function commentsOf(document: Document): Map<string, number> {
    const walker = document.createTreeWalker(document, NodeFilter.SHOW_COMMENT);
    const comments = new Map<string, number>();
    while (walker.nextNode()) {
        const text = walker.currentNode.nodeValue ?? '';
        comments.set(text, (comments.get(text) ?? 0) + 1);
    }
    return comments;
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
