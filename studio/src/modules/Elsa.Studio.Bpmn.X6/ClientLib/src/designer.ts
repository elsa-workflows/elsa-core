/**
 * The X6 BPMN designer: BPMN designer contract version 1 on AntV X6 and the shared modeling kernel.
 *
 * The kernel's session holds the document and its undo history; X6 only draws it, one plane at a time, and is redrawn
 * from the document after every edit. A gesture becomes a kernel command: dragging a shape moves it, and the keyboard
 * undoes and redoes. Elements are identified by their BPMN element id everywhere.
 */
import { Graph } from '@antv/x6';
import type {
    BpmnActivityBinding,
    BpmnChangeEvent,
    BpmnDesigner,
    BpmnDesignerOptions,
    BpmnElementMarker,
    BpmnElementStats,
    BpmnLoadResult,
    BpmnOverlayResult,
    BpmnSaveResult,
    BpmnSelectionEvent,
    BpmnSetBindingResult
} from '../../../Elsa.Studio.Bpmn/ClientLib/src/index.js';
import { BpmnModelingSession, type Bounds, moveShapes, type Point } from '../../../Elsa.Studio.Bpmn/ClientLib/src/modeling/index.js';
import { type PlaneCells, planeCells } from './cells.js';
import { indexPlanes, type PlaneIndex } from './planes.js';
import { registerShapes, SHAPES } from './shapes.js';

const CONTAINER_CLASS = 'elsa-x6-designer';
const STATE_CLASSES = ['elsa-faulted', 'elsa-blocked', 'elsa-active', 'elsa-completed', 'elsa-canceled'];
const PROBLEM_CLASSES = ['elsa-problem-error', 'elsa-problem-warning'];
const SELECTED_CLASS = 'elsa-selected';
const DISPOSED = 'This BPMN designer has been disposed.';

export function createDesigner(container: HTMLElement, options: BpmnDesignerOptions = {}): BpmnDesigner {
    if (!(container instanceof HTMLElement)) throw new TypeError('A BPMN designer needs a container element.');

    const readOnly = options.readOnly === true;
    // The designer renders no properties panel, so it ignores `propertiesContainer` and leaves the host's content in it:
    // the host's activity picker and input editors edit bindings.

    registerShapes();
    container.classList.add(CONTAINER_CLASS);
    const breadcrumbs = document.createElement('nav');
    breadcrumbs.className = 'elsa-x6-breadcrumbs';
    breadcrumbs.setAttribute('aria-label', 'Diagram');
    breadcrumbs.hidden = true;
    const canvas = document.createElement('div');
    canvas.className = 'elsa-x6-canvas';
    // Focusable, so the keyboard shortcuts reach it.
    canvas.tabIndex = 0;
    container.append(breadcrumbs, canvas);

    let graph: Graph | null = new Graph({
        container: canvas,
        autoResize: true,
        async: false,
        interacting: readOnly ? false : { nodeMovable: view => isMovable(String(view.cell.id)) },
        panning: { enabled: true },
        mousewheel: { enabled: true, modifiers: ['ctrl', 'meta'], factor: 1.1 }
    });
    const session = new BpmnModelingSession({ readOnly });

    const listeners = {
        change: new Set<(event: BpmnChangeEvent) => void>(),
        selection: new Set<(event: BpmnSelectionEvent) => void>()
    };
    let planes: PlaneIndex | null = null;
    let currentKey: string | undefined;
    let drawn: PlaneCells | null = null;
    let selected: string | null = null;
    // A plane drawn while the container had no size is fitted once it has one, such as when a hidden tab is shown.
    let fitPending = false;
    // Kept by element id across plane changes and edits, and drawn for the elements the current plane shows.
    const stats = new Map<string, BpmnElementStats>();
    const markers = new Map<string, BpmnElementMarker[]>();
    // The shape being dragged, and where each cell that follows it started.
    let dragging: { elementId: string; start: Point; followers: Map<string, Point> } | null = null;
    // Loads run one after another, so the last one called is the one that shows.
    let loading: Promise<unknown> = Promise.resolve();
    let disposed = false;

    function ensureLive(): Graph {
        if (disposed || !graph) throw new Error(DISPOSED);
        return graph;
    }

    function emit(kind: 'change', payload: BpmnChangeEvent): void;
    function emit(kind: 'selection', payload: BpmnSelectionEvent): void;
    function emit(kind: keyof typeof listeners, payload: unknown): void {
        for (const listener of [...listeners[kind]] as ((payload: unknown) => void)[]) {
            try {
                listener(payload);
            } catch (error) {
                console.error(`elsa-x6-designer: a ${kind} listener threw.`, error);
            }
        }
    }

    const unsubscribeSession = session.onChange(event => {
        // An edit can rename or remove what a plane draws, so the planes are rebuilt from the document.
        planes = indexPlanes(session.document!);
        if (!currentKey || !planes.plane(currentKey)) currentKey = planes.rootKey;
        if (selected && !planes.planeOfElement(selected)) selected = null;
        render(false);
        emit('change', event);
    });

    /** Draws the current plane from the document, with its badges, markers and selection. */
    function render(fit: boolean): void {
        const target = graph!;
        const document = session.document;
        const plane = document && planes && currentKey ? planes.plane(currentKey) : undefined;
        if (!document || !plane) {
            drawn = null;
            target.resetCells([]);
            renderBreadcrumbs();
            return;
        }
        drawn = planeCells(document, plane, subprocessId => planes?.drillTarget(subprocessId) !== undefined);
        target.resetCells([...drawn.nodes.map(node => target.createNode(node)), ...drawn.edges.map(edge => target.createEdge(edge))]);
        decorate();
        renderBreadcrumbs();
        if (fit) fitToView();
    }

    function fitToView(): void {
        fitPending = canvas.clientWidth === 0 || canvas.clientHeight === 0;
        if (!fitPending) graph!.zoomToFit({ padding: 24, maxScale: 1 });
    }

    /** Redraws the badges, problem markers and selection of the elements on the current plane. */
    function decorate(): void {
        const target = graph!;
        if (!drawn) return;
        target.removeCells(target.getCells().filter(cell => /::(badge|problem)$/.test(cell.id)));
        for (const elementId of drawn.bounds.keys()) {
            const view = target.findViewByCell(elementId);
            if (!view) continue;
            view.removeClass([...STATE_CLASSES, ...PROBLEM_CLASSES, SELECTED_CLASS]);
            const elementStats = stats.get(elementId);
            const bounds = drawn.bounds.get(elementId)!;
            if (elementStats) {
                const state = stateOf(elementStats);
                if (state !== 'idle') view.addClass(`elsa-${state}`);
                target.addNode(badgeCell(elementId, bounds, drawn.midpoints.get(elementId), elementStats, state));
            }
            const elementMarkers = markers.get(elementId);
            if (elementMarkers) {
                const severity = elementMarkers.some(marker => severityOf(marker) === 'error') ? 'error' : 'warning';
                view.addClass(`elsa-problem-${severity}`);
                target.addNode(problemCell(elementId, bounds, drawn.midpoints.get(elementId), elementMarkers, severity));
            }
            if (elementId === selected) view.addClass(SELECTED_CLASS);
        }
    }

    function renderBreadcrumbs(): void {
        const path = planes && currentKey ? planes.path(currentKey) : [];
        breadcrumbs.replaceChildren();
        breadcrumbs.hidden = path.length < 2;
        path.forEach((plane, index) => {
            if (index > 0) breadcrumbs.append(Object.assign(document.createElement('span'), { className: 'elsa-x6-breadcrumb-separator', textContent: '›' }));
            const crumb = document.createElement('button');
            crumb.type = 'button';
            crumb.className = 'elsa-x6-breadcrumb';
            crumb.textContent = plane.title;
            crumb.dataset.planeKey = plane.key;
            if (index === path.length - 1) crumb.setAttribute('aria-current', 'page');
            else crumb.addEventListener('click', () => openPlane(plane.key));
            breadcrumbs.append(crumb);
        });
    }

    /** Shows another plane. The selection is cleared when the element it names is not on that plane. */
    function openPlane(key: string | undefined): void {
        if (disposed || !planes || !key || key === currentKey || !planes.plane(key)) return;
        currentKey = key;
        render(true);
        if (selected !== null && !drawn?.bounds.has(selected)) setSelection(null);
    }

    function setSelection(elementId: string | null): void {
        selected = elementId;
        decorate();
        const element = elementId ? session.document?.element(elementId) : undefined;
        const name = typeof element?.name === 'string' && element.name !== '' ? element.name : undefined;
        emit('selection', {
            elementIds: elementId ? [elementId] : [],
            primary: elementId ? { id: elementId, type: String(element?.$type ?? drawn?.types.get(elementId) ?? ''), ...(name ? { name } : {}) } : null
        });
    }

    /** The element a cell stands for: a label or badge stands for the element it belongs to. */
    const elementOf = (cellId: string) => cellId.split('::')[0];

    /** The diagram plane the current plane's shapes are on: a subprocess's hidden content is on its parent's plane. */
    const diagramPlaneId = () => currentKey?.split('/')[0];

    /**
     * Whether a cell is a shape the user can drag. A lane moves with its participant and a boundary event with its
     * activity, as the kernel requires; a label, badge or edge does not move on its own.
     */
    function isMovable(cellId: string): boolean {
        const type = drawn?.types.get(cellId);
        return !cellId.includes('::') && type !== undefined && !drawn!.midpoints.has(cellId) && type !== 'bpmn:Lane' && type !== 'bpmn:BoundaryEvent';
    }

    // While a shape is dragged, what moves with it follows on screen; its edges are redrawn from the document on drop.
    graph.on('node:move', ({ node }) => {
        const elementId = String(node.id);
        const document = session.document;
        const planeId = diagramPlaneId();
        const start = drawn?.bounds.get(elementId);
        if (!document || !planeId || !start || !isMovable(elementId)) return;
        const moved = new Set(moveShapes(document, planeId, [elementId], { x: 0, y: 0 }).elementIds);
        const followers = new Map<string, Point>();
        for (const cell of graph!.getNodes()) {
            const id = String(cell.id);
            if (id !== elementId && moved.has(elementOf(id)) && !drawn!.midpoints.has(elementOf(id))) followers.set(id, cell.getPosition());
        }
        dragging = { elementId, start: { x: start.x, y: start.y }, followers };
    });
    graph.on('node:change:position', ({ node, options }) => {
        if (!dragging || String(node.id) !== dragging.elementId || !options.ui) return;
        const { x, y } = node.getPosition();
        const delta = { x: x - dragging.start.x, y: y - dragging.start.y };
        for (const [id, from] of dragging.followers) graph!.getCellById(id)?.setProp('position', { x: from.x + delta.x, y: from.y + delta.y });
    });
    graph.on('node:moved', ({ node }) => {
        const current = dragging;
        dragging = null;
        const document = session.document;
        const planeId = diagramPlaneId();
        if (!current || String(node.id) !== current.elementId || !document || !planeId) return;
        const { x, y } = node.getPosition();
        const delta = { x: Math.round(x - current.start.x), y: Math.round(y - current.start.y) };
        if (current.elementId !== selected) setSelection(current.elementId);
        if (delta.x === 0 && delta.y === 0) {
            render(false);
            return;
        }
        try {
            session.execute(moveShapes(document, planeId, [current.elementId], delta));
        } catch (error) {
            console.error('elsa-x6-designer: a move was refused.', error);
            render(false);
        }
    });

    // Ctrl+Z (Cmd+Z) undoes; Ctrl+Shift+Z and Ctrl+Y redo.
    canvas.addEventListener('keydown', event => {
        if (readOnly || !(event.ctrlKey || event.metaKey) || event.altKey) return;
        const key = event.key.toLowerCase();
        const redo = key === 'y' || (key === 'z' && event.shiftKey);
        if (key !== 'z' && !redo) return;
        event.preventDefault();
        if (redo) session.redo();
        else session.undo();
    });
    graph.on('cell:mousedown', () => canvas.focus({ preventScroll: true }));
    graph.on('blank:mousedown', () => canvas.focus({ preventScroll: true }));

    graph.on('cell:click', ({ cell }) => {
        const elementId = elementOf(String(cell.id));
        if (drawn?.bounds.has(elementId) && elementId !== selected) setSelection(elementId);
    });
    graph.on('blank:click', () => {
        if (selected !== null) setSelection(null);
    });
    graph.on('node:drill', ({ node }: { node: { id: string } }) => openPlane(planes?.drillTarget(elementOf(String(node.id)))));
    graph.on('resize', () => {
        if (fitPending && drawn) fitToView();
    });
    graph.on('node:dblclick', ({ node }) => openPlane(planes?.drillTarget(elementOf(String(node.id)))));

    async function load(xml: string): Promise<BpmnLoadResult> {
        const result = await session.load(xml);
        if (disposed || !result.imported) return result;
        stats.clear();
        markers.clear();
        selected = null;
        planes = indexPlanes(session.document!);
        currentKey = planes.rootKey;
        render(true);
        return result;
    }

    // Methods never use `this`: a JS interop host may call them detached from the designer.
    function clearOverlays(): BpmnOverlayResult {
        ensureLive();
        stats.clear();
        decorate();
        return { applied: [], unknownElementIds: [] };
    }

    function clearMarkers(): BpmnOverlayResult {
        ensureLive();
        markers.clear();
        decorate();
        return { applied: [], unknownElementIds: [] };
    }

    /** Whether a plane draws the element: only those can show overlays and markers or be selected. */
    const isDrawn = (elementId: string) => planes?.planeOfElement(elementId) !== undefined;

    return {
        load(xml: string): Promise<BpmnLoadResult> {
            ensureLive();
            const result = loading.then(() => load(xml));
            loading = result.catch(() => undefined);
            return result;
        },

        save(): Promise<BpmnSaveResult> {
            ensureLive();
            return session.save();
        },

        select(elementId: string | null): boolean {
            const target = ensureLive();
            if (elementId === null) {
                setSelection(null);
                return true;
            }
            const key = planes?.planeOfElement(elementId);
            if (!key) return false;
            if (key !== currentKey) {
                currentKey = key;
                render(true);
            }
            setSelection(elementId);
            const cell = target.getCellById(elementId);
            if (cell) target.centerCell(cell);
            return true;
        },

        getBinding(elementId: string): BpmnActivityBinding | null {
            ensureLive();
            return session.getBinding(elementId);
        },

        setBinding(elementId: string, binding: BpmnActivityBinding | null): BpmnSetBindingResult {
            ensureLive();
            return session.setBinding(elementId, binding);
        },

        undo(): boolean {
            ensureLive();
            return session.undo();
        },

        redo(): boolean {
            ensureLive();
            return session.redo();
        },

        applyOverlays(statsById: Record<string, BpmnElementStats> | null): BpmnOverlayResult {
            ensureLive();
            stats.clear();
            const applied: string[] = [];
            const unknownElementIds: string[] = [];
            for (const [elementId, elementStats] of Object.entries(statsById ?? {})) {
                if (!isDrawn(elementId)) {
                    unknownElementIds.push(elementId);
                    continue;
                }
                stats.set(elementId, elementStats);
                applied.push(elementId);
            }
            decorate();
            return { applied, unknownElementIds };
        },

        clearOverlays,

        applyMarkers(list: BpmnElementMarker[] | null): BpmnOverlayResult {
            ensureLive();
            for (const marker of list ?? []) {
                if (typeof marker?.elementId !== 'string' || typeof marker.severity !== 'string' || typeof marker.message !== 'string')
                    throw new TypeError('A BPMN element marker needs a string elementId, severity and message.');
            }
            markers.clear();
            const applied: string[] = [];
            const unknownElementIds: string[] = [];
            // Each element once, in the order the list first names it, with all of its markers.
            for (const marker of list ?? []) {
                if (markers.has(marker.elementId)) {
                    markers.get(marker.elementId)!.push(marker);
                } else if (!isDrawn(marker.elementId)) {
                    if (!unknownElementIds.includes(marker.elementId)) unknownElementIds.push(marker.elementId);
                } else {
                    markers.set(marker.elementId, [marker]);
                    applied.push(marker.elementId);
                }
            }
            decorate();
            return { applied, unknownElementIds };
        },

        clearMarkers,

        on(event: 'change' | 'selection', listener: ((event: BpmnChangeEvent) => void) | ((event: BpmnSelectionEvent) => void)): () => void {
            ensureLive();
            const set = listeners[event] as Set<unknown> | undefined;
            if (!set) throw new Error(`Unknown BPMN designer event '${String(event)}'.`);
            set.add(listener);
            return () => {
                set.delete(listener);
            };
        },

        dispose(): void {
            if (disposed) return;
            disposed = true;
            unsubscribeSession();
            listeners.change.clear();
            listeners.selection.clear();
            stats.clear();
            markers.clear();
            graph!.dispose();
            graph = null;
            container.replaceChildren();
            container.classList.remove(CONTAINER_CLASS);
        }
    } satisfies BpmnDesigner;
}

type ElementState = 'faulted' | 'blocked' | 'active' | 'canceled' | 'completed' | 'idle';

/** The one state an element shows, most urgent first, as the bpmn-js designer orders it. */
function stateOf(stats: BpmnElementStats): ElementState {
    if (stats.faulted) return 'faulted';
    if (stats.canceled) return 'canceled';
    if (stats.blocked) return 'blocked';
    if ((stats.active ?? 0) > 0) return 'active';
    if ((stats.completed ?? 0) > 0) return 'completed';
    return 'idle';
}

/** The number a badge shows for its state, as the bpmn-js designer shows it: a running element counts its active tokens. */
function countOf(stats: BpmnElementStats, state: ElementState): number {
    switch (state) {
        case 'active':
            return stats.active ?? 0;
        case 'completed':
            return stats.completed ?? 0;
        case 'idle':
            return stats.completed ?? stats.started ?? 0;
        default:
            return stats.started ?? stats.completed ?? 0;
    }
}

/** A marker's severity as drawn: one the designer does not know shows as an error. */
function severityOf(marker: BpmnElementMarker): 'error' | 'warning' {
    return marker.severity === 'warning' ? 'warning' : 'error';
}

/** The instance-state badge at a shape's top right, as the bpmn-js designer places it, or above an edge's midpoint. */
function badgeCell(elementId: string, bounds: Bounds, midpoint: Point | undefined, stats: BpmnElementStats, state: ElementState) {
    const count = String(countOf(stats, state));
    const title = `Started ${stats.started ?? 0}, completed ${stats.completed ?? 0}, active ${stats.active ?? 0}`;
    const width = Math.max(18, 10 + count.length * 7);
    return {
        id: `${elementId}::badge`,
        shape: SHAPES.badge,
        x: midpoint ? midpoint.x + 4 : bounds.x + bounds.width - 10,
        y: midpoint ? midpoint.y - 22 : bounds.y - 10,
        width,
        height: 18,
        zIndex: 400,
        attrs: {
            // X6's `title` attribute writes a <title> child, which browsers show as a tooltip.
            group: { title, 'aria-label': title, role: 'img', class: `elsa-x6-badge elsa-x6-badge-${state}` },
            body: { fill: `var(--elsa-bpmn-state-${state})` },
            label: { text: count }
        }
    };
}

/** The problem badge at a shape's bottom left, or on an edge's midpoint, naming every message. */
function problemCell(elementId: string, bounds: Bounds, midpoint: Point | undefined, markers: BpmnElementMarker[], severity: 'error' | 'warning') {
    const title = markers.map(marker => marker.message).join('\n');
    return {
        id: `${elementId}::problem`,
        shape: SHAPES.problem,
        x: midpoint ? midpoint.x - 9 : bounds.x - 10,
        y: midpoint ? midpoint.y - 9 : bounds.y + bounds.height - 10,
        width: 18,
        height: 18,
        zIndex: 400,
        attrs: {
            group: { title, 'aria-label': title, role: 'img', class: `elsa-x6-problem elsa-x6-problem-${severity}` },
            body: { fill: `var(--elsa-bpmn-problem-${severity})` }
        }
    };
}
