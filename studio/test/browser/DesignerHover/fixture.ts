import {Graph} from '@antv/x6';
import '@antv/x6/dist/index.css';
import {registerEdgeHoverTools} from '../../../src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/designer/api/edge-hover-tools';

// All interactions come from Chromium's mouse. Supplemental probes explicitly drop selected helper
// notifications; the nine primary cases subscribe directly, without this fault-injection adapter.
const container = document.getElementById('graph')!;
let graph: Graph;
let events: Record<string, number>;
let lastTrustedPointer: {x: number; y: number} | null = null;
let observationController: AbortController;
type ProbeOptions = {target: 'edge' | 'node'; omitTarget: boolean};
type OldEdgeState = {model_buttons: number; rendered_buttons: number; model_vertices: number; rendered_vertices: number};
let probe: (ProbeOptions & {
    event_order: string[];
    order_overflow: boolean;
    a_event_index: number;
    suppressed_edge_leaves: number;
    suppressed_blank_overs: number;
    target_callbacks: number;
    target_entry: {event_index: number; before: OldEdgeState; after: OldEdgeState; delivered: boolean;
        suppressed_edge_leaves: number; suppressed_blank_overs: number} | null;
}) | null;

function edgeTools(id: string) {
    const edge = graph.getCellById(id);
    const items = edge?.getTools()?.items ?? [];
    const names = items.map(item => typeof item === 'string' ? item : item.name);
    return {
        id,
        exists: Boolean(edge),
        buttons: names.filter(name => name === 'button-remove').length,
        vertices: names.filter(name => name === 'vertices').length,
    };
}

function oldEdgeState(): OldEdgeState {
    const edge = edgeTools('edge-a');
    const tools = '.x6-cell-tools[data-cell-id="edge-a"]';
    return {
        model_buttons: edge.buttons,
        rendered_buttons: container.querySelectorAll(`${tools} [data-tool-name="button-remove"]`).length,
        model_vertices: edge.vertices,
        rendered_vertices: container.querySelectorAll(`${tools} [data-tool-name="vertices"]`).length,
    };
}

function reset(enabled: boolean, options: ProbeOptions | null = null) {
    observationController?.abort();
    graph?.dispose();
    events = {};
    lastTrustedPointer = null;
    probe = options ? {...options, event_order: [], order_overflow: false, a_event_index: -1,
        suppressed_edge_leaves: 0, suppressed_blank_overs: 0, target_callbacks: 0, target_entry: null} : null;
    graph = new Graph({
        container,
        width: 800,
        height: 440,
        async: false,
        interacting: enabled,
        background: {color: '#fafafa'},
    });
    // Observe real X6 notifications before helper callbacks, including callbacks omitted by probes.
    for (const event of ['edge:mouseenter', 'edge:mouseleave', 'node:mouseenter', 'blank:mouseover', 'graph:mouseleave'] as const) {
        graph.on(event, (args: {edge?: {id: string}}) => {
            const key = args.edge ? `${event}:${args.edge.id}` : event;
            events[key] = (events[key] ?? 0) + 1;
            if (probe) {
                if (probe.event_order.length < 64) {
                    probe.event_order.push(key);
                } else {
                    probe.order_overflow = true;
                }
            }
        });
    }
    if (probe) {
        const on: Graph['on'] = (name: string, handler: Parameters<Graph['on']>[1], context?: unknown) =>
            graph.on(name, (...args: Parameters<typeof handler>) => {
                const state = probe!;
                if (name === 'edge:mouseleave' || name === 'blank:mouseover') {
                    // Drop only the helper's earlier fallback callbacks, retaining independent events.
                    if (state.a_event_index >= 0) {
                        if (name === 'edge:mouseleave') {
                            state.suppressed_edge_leaves += 1;
                        } else {
                            state.suppressed_blank_overs += 1;
                        }
                    }
                    return;
                }
                const edgeId = args[0]?.edge?.id;
                const target = state.target === 'edge'
                    ? name === 'edge:mouseenter' && edgeId === 'edge-b'
                    : name === 'node:mouseenter';
                if (target) {
                    state.target_callbacks += 1;
                    const before = oldEdgeState();
                    const event_index = state.event_order.length - 1;
                    const suppressed_edge_leaves = state.suppressed_edge_leaves;
                    const suppressed_blank_overs = state.suppressed_blank_overs;
                    // Negative controls omit only the destination callback. A's genuine entry is delivered.
                    // X6 invokes handlers with their registered context. Forward that exact context.
                    const result = state.omitTarget ? undefined : handler.apply(context, args);
                    state.target_entry ??= {event_index, before, after: oldEdgeState(), delivered: !state.omitTarget,
                        suppressed_edge_leaves, suppressed_blank_overs};
                    return result;
                }
                const eventIndex = state.event_order.length - 1;
                const result = handler.apply(context, args);
                if (name === 'edge:mouseenter' && edgeId === 'edge-a') {
                    state.a_event_index = eventIndex;
                    state.suppressed_edge_leaves = 0;
                    state.suppressed_blank_overs = 0;
                }
                return result;
            }, context);
        registerEdgeHoverTools({on}, enabled);
    } else {
        registerEdgeHoverTools(graph, enabled);
    }
    // X6 handles mouse/touch events. Observe untouched trusted pointer events at the actual root
    // in capture phase, independently of its emulated mouseenter/leave and descendant events.
    observationController = new AbortController();
    const observeBoundary = (observed: PointerEvent) => {
        if (!observed.isTrusted || observed.target !== container || observed.pointerType !== 'mouse') {
            return;
        }
        const key = `container:${observed.type}`;
        events[key] = (events[key] ?? 0) + 1;
    };
    const observationOptions = {capture: true, signal: observationController.signal};
    container.addEventListener('pointerenter', observeBoundary, observationOptions);
    container.addEventListener('pointerleave', observeBoundary, observationOptions);
    document.addEventListener('pointermove', observed => {
        if (observed.isTrusted && observed.pointerType === 'mouse') {
            lastTrustedPointer = {x: observed.clientX, y: observed.clientY};
        }
    }, observationOptions);
    graph.addNode({id: 'node', x: 700, y: 90, width: 80, height: 60, label: 'Node'});
    graph.addEdge({id: 'edge-a', source: {x: 100, y: 120}, target: {x: 660, y: 120}});
    // B reaches the boundary, allowing a direct edge-to-outside transition without crossing blank canvas.
    graph.addEdge({id: 'edge-b', source: {x: 100, y: 240}, target: {x: 800, y: 240}});
}

function snapshot() {
    const rect = container.getBoundingClientRect();
    const point = lastTrustedPointer;
    const hit = point ? document.elementFromPoint(point.x, point.y) : null;
    return {
        events: {...events},
        pointerInside: container.matches(':hover'),
        bounds: {width: rect.width, height: rect.height},
        trustedPointerSeen: point !== null,
        trustedPointerInsideRect: point !== null && point.x >= rect.left && point.x < rect.right && point.y >= rect.top && point.y < rect.bottom,
        trustedPointerHitsGraph: hit !== null && container.contains(hit),
        edges: ['edge-a', 'edge-b'].map(edgeTools),
        probe,
    };
}

reset(true);
Object.assign(window, {hoverFixture: {reset, snapshot}});
window.addEventListener('pagehide', () => {
    observationController.abort();
    graph.dispose();
}, {once: true});
