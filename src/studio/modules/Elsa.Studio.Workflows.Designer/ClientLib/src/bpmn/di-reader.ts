/**
 * Reads BPMN DI geometry -- and nothing else -- out of a BPMN 2.0 document.
 *
 * BPMN DI is not on the `Elsa.BpmnProcess` activity payload: in the payload schema, shapes and
 * edges hang off `bpmnDefinitions.diagrams`, while the activity's `process` property is a
 * `bpmnProcessDefinition` with no diagram section at all. The one place Studio can get real
 * coordinates from is the document the definition was imported from, which elsa-core stores on the
 * workflow definition's `CustomProperties["Bpmn:SourceXml"]`.
 *
 * **This reader is geometry-only, on purpose.** It reads `bpmndi:BPMNShape`, `bpmndi:BPMNEdge` and
 * their bounds, waypoints and labels, plus the participant names on `bpmn:collaboration` that a
 * pool needs to render -- which are likewise absent from a payload rooted at a single process.
 * It never reads elements, flows, lanes or bindings: the JSON is the typed source of truth for
 * structure, and letting this file grow into a second BPMN reader is exactly the failure that
 * would put two disagreeing views of the same document into Studio.
 *
 * Parsing goes through the platform `DOMParser`, which the browser provides at runtime and Vitest's
 * jsdom environment provides in tests.
 */

const NS_DI = 'http://www.omg.org/spec/BPMN/20100524/DI';
const NS_DC = 'http://www.omg.org/spec/DD/20100524/DC';
const NS_DD_DI = 'http://www.omg.org/spec/DD/20100524/DI';
const NS_MODEL = 'http://www.omg.org/spec/BPMN/20100524/MODEL';

/** Bounds as BPMN DI states them. */
export interface DiBounds {
    x: number;
    y: number;
    width: number;
    height: number;
}

export interface DiShape {
    readonly elementId: string;
    readonly bounds: DiBounds;
    readonly labelBounds: DiBounds | null;
    readonly isExpanded: boolean | null;
    readonly isHorizontal: boolean | null;
    readonly isMarkerVisible: boolean | null;
}

export interface DiEdge {
    readonly elementId: string;
    readonly waypoints: readonly { x: number; y: number }[];
    readonly labelBounds: DiBounds | null;
}

export interface DiParticipant {
    readonly id: string;
    readonly name: string | null;
    readonly processId: string | null;
}

/** What one source document yielded. */
export interface BpmnDiagramInterchange {
    /** Whether the document contained at least one `bpmndi:BPMNDiagram` with a plane. */
    readonly hasDiagram: boolean;
    readonly shapes: ReadonlyMap<string, DiShape>;
    readonly edges: ReadonlyMap<string, DiEdge>;
    readonly participants: readonly DiParticipant[];
    /** Element ids claimed by more than one shape; the first shape in document order was kept. */
    readonly duplicateShapeIds: readonly string[];
    /** Set when the document could not be parsed as XML at all. Everything else is then empty. */
    readonly parseError: string | null;
}

const EMPTY: BpmnDiagramInterchange = {
    hasDiagram: false,
    shapes: new Map(),
    edges: new Map(),
    participants: [],
    duplicateShapeIds: [],
    parseError: null,
};

/**
 * Reads every diagram in the document into one geometry index keyed by the id of the element each
 * shape or edge draws.
 *
 * All planes are merged. A modeller writes a collapsed subprocess's body into a plane of its own,
 * and an expanded one into the same plane as its parent; since the payload already says which scope
 * an element belongs to, which plane its geometry was written on carries no extra information.
 */
export function readDiagramInterchange(sourceXml: string | null | undefined): BpmnDiagramInterchange {
    if (sourceXml == null || sourceXml.trim().length === 0) return EMPTY;

    let document: Document;

    try {
        document = new DOMParser().parseFromString(sourceXml, 'application/xml');
    } catch (error) {
        return { ...EMPTY, parseError: error instanceof Error ? error.message : String(error) };
    }

    const parserError = findParserError(document);

    if (parserError != null) return { ...EMPTY, parseError: parserError };

    const shapes = new Map<string, DiShape>();
    const edges = new Map<string, DiEdge>();
    const duplicateShapeIds: string[] = [];
    const planes = Array.from(document.getElementsByTagNameNS(NS_DI, 'BPMNPlane'));

    for (const plane of planes) {
        for (const shapeElement of childrenNS(plane, NS_DI, 'BPMNShape')) {
            const elementId = trimmedAttribute(shapeElement, 'bpmnElement');
            const bounds = readBounds(firstChildNS(shapeElement, NS_DC, 'Bounds'));

            if (elementId == null || bounds == null) continue;

            if (shapes.has(elementId)) {
                duplicateShapeIds.push(elementId);
                continue;
            }

            shapes.set(elementId, {
                elementId,
                bounds,
                labelBounds: readLabelBounds(shapeElement),
                isExpanded: readBooleanAttribute(shapeElement, 'isExpanded'),
                isHorizontal: readBooleanAttribute(shapeElement, 'isHorizontal'),
                isMarkerVisible: readBooleanAttribute(shapeElement, 'isMarkerVisible'),
            });
        }

        for (const edgeElement of childrenNS(plane, NS_DI, 'BPMNEdge')) {
            const elementId = trimmedAttribute(edgeElement, 'bpmnElement');

            if (elementId == null || edges.has(elementId)) continue;

            const waypoints: { x: number; y: number }[] = [];

            for (const waypoint of childrenNS(edgeElement, NS_DD_DI, 'waypoint')) {
                const x = readNumber(waypoint, 'x');
                const y = readNumber(waypoint, 'y');

                if (x != null && y != null) waypoints.push({ x, y });
            }

            edges.set(elementId, { elementId, waypoints, labelBounds: readLabelBounds(edgeElement) });
        }
    }

    const participants: DiParticipant[] = [];

    for (const collaboration of Array.from(document.getElementsByTagNameNS(NS_MODEL, 'collaboration'))) {
        for (const participant of childrenNS(collaboration, NS_MODEL, 'participant')) {
            const id = trimmedAttribute(participant, 'id');

            if (id == null) continue;

            participants.push({
                id,
                name: trimmedAttribute(participant, 'name'),
                processId: trimmedAttribute(participant, 'processRef'),
            });
        }
    }

    return { hasDiagram: planes.length > 0, shapes, edges, participants, duplicateShapeIds, parseError: null };
}

/**
 * The namespaces a `DOMParser` puts its own `parsererror` element in. Chromium and WebKit use XHTML;
 * Gecko -- and jsdom, which follows it -- use Mozilla's own. A null namespace is accepted too,
 * because an engine that emits a bare `parsererror` is still reporting a parse failure.
 */
const PARSER_ERROR_NAMESPACES: readonly (string | null)[] = [
    null,
    'http://www.w3.org/1999/xhtml',
    'http://www.mozilla.org/newlayout/xml/parsererror.xml',
];

/**
 * A `DOMParser` reports a malformed document by *returning* one whose content is a `parsererror`
 * element rather than by throwing, so every caller has to look for it explicitly. Checking the
 * namespace as well as the local name keeps a BPMN document that legitimately contains an element
 * called `parsererror` -- retained foreign content, say -- from being read as a parse failure.
 */
function findParserError(document: Document): string | null {
    for (const error of Array.from(document.getElementsByTagName('parsererror'))) {
        if (PARSER_ERROR_NAMESPACES.includes(error.namespaceURI)) {
            return (error.textContent ?? 'The BPMN source could not be parsed as XML.').trim();
        }
    }

    return null;
}

function childrenNS(parent: Element, namespaceUri: string, localName: string): Element[] {
    const matches: Element[] = [];

    for (const node of Array.from(parent.childNodes)) {
        if (node.nodeType !== 1) continue;

        const element = node as Element;

        if (element.namespaceURI === namespaceUri && element.localName === localName) matches.push(element);
    }

    return matches;
}

function firstChildNS(parent: Element, namespaceUri: string, localName: string): Element | null {
    return childrenNS(parent, namespaceUri, localName)[0] ?? null;
}

/** A `BPMNLabel` carries its own `dc:Bounds`; a label with no bounds is a label the modeller left where it fell. */
function readLabelBounds(owner: Element): DiBounds | null {
    const label = firstChildNS(owner, NS_DI, 'BPMNLabel');

    return label == null ? null : readBounds(firstChildNS(label, NS_DC, 'Bounds'));
}

function readBounds(element: Element | null): DiBounds | null {
    if (element == null) return null;

    const x = readNumber(element, 'x');
    const y = readNumber(element, 'y');
    const width = readNumber(element, 'width');
    const height = readNumber(element, 'height');

    if (x == null || y == null || width == null || height == null) return null;

    return { x, y, width, height };
}

function readNumber(element: Element, name: string): number | null {
    const raw = trimmedAttribute(element, name);

    if (raw == null) return null;

    const value = Number(raw);

    return Number.isFinite(value) ? value : null;
}

function readBooleanAttribute(element: Element, name: string): boolean | null {
    const raw = trimmedAttribute(element, name);

    if (raw == null) return null;

    const lowered = raw.toLowerCase();

    return lowered === 'true' ? true : lowered === 'false' ? false : null;
}

function trimmedAttribute(element: Element, name: string): string | null {
    const raw = element.getAttribute(name);

    if (raw == null) return null;

    const trimmed = raw.trim();

    return trimmed.length === 0 ? null : trimmed;
}
