import {Graph} from '@antv/x6';
import '@antv/x6/dist/index.css';
import {registerEdgeHoverTools} from '../../../../src/studio/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/designer/api/edge-hover-tools';

// Expose observations and fresh-graph setup only. All acceptance interactions come from Chromium's mouse.
const container = document.getElementById('graph')!;
let graph: Graph;
let events: Record<string, number>;
let pointerInside = false;
let observationController: AbortController;

function reset(enabled: boolean) {
    observationController?.abort();
    graph?.dispose();
    events = {};
    pointerInside = container.matches(':hover');
    graph = new Graph({
        container,
        width: 800,
        height: 440,
        async: false,
        interacting: enabled,
        background: {color: '#fafafa'},
    });
    registerEdgeHoverTools(graph, enabled);
    for (const event of ['edge:mouseenter', 'edge:mouseleave', 'node:mouseenter', 'blank:mouseover', 'graph:mouseleave'] as const) {
        graph.on(event, (args: {edge?: {id: string}}) => {
            const key = args.edge ? `${event}:${args.edge.id}` : event;
            events[key] = (events[key] ?? 0) + 1;
        });
    }
    // X6 emulates leave from mouseout and can retain a descendant edge as its event target.
    // Observe the actual container boundary independently; never manufacture a graph event.
    observationController = new AbortController();
    for (const event of ['mouseenter', 'mouseleave'] as const) {
        container.addEventListener(event, () => {
            pointerInside = event === 'mouseenter';
            const key = `container:${event}`;
            events[key] = (events[key] ?? 0) + 1;
        }, {signal: observationController.signal});
    }
    graph.addNode({id: 'node', x: 700, y: 90, width: 80, height: 60, label: 'Node'});
    graph.addEdge({id: 'edge-a', source: {x: 100, y: 120}, target: {x: 660, y: 120}});
    // B reaches the boundary, allowing a direct edge-to-outside transition without crossing blank canvas.
    graph.addEdge({id: 'edge-b', source: {x: 100, y: 240}, target: {x: 800, y: 240}});
}

function snapshot() {
    return {
        events: {...events},
        pointerInside,
        edges: ['edge-a', 'edge-b'].map(id => {
            const edge = graph.getCellById(id);
            const items = edge?.getTools()?.items ?? [];
            const names = items.map(item => typeof item === 'string' ? item : item.name);
            return {
                id,
                exists: Boolean(edge),
                buttons: names.filter(name => name === 'button-remove').length,
                vertices: names.filter(name => name === 'vertices').length,
            };
        }),
    };
}

reset(true);
Object.assign(window, {hoverFixture: {reset, snapshot}});
window.addEventListener('pagehide', () => {
    observationController.abort();
    graph.dispose();
}, {once: true});
