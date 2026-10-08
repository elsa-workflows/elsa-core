import {Edge, Graph, Node} from '@antv/x6';

const AccessibilityMarker = 'data-elsa-state-machine-a11y';

export function applyStateMachineGraphAccessibility(graph: Graph) {
    graph.getNodes().forEach(node => applyStateMachineNodeAccessibility(graph, node));
    graph.getEdges().forEach(edge => applyStateMachineEdgeAccessibility(graph, edge));
}

export function applyStateMachineNodeAccessibility(graph: Graph, node: Node) {
    const accessibleName = node.getData()?.accessibleName
        ?? `${node.attr('title/text') || 'Unnamed'}, state`;
    applyStateMachineCellAccessibility(graph, node, accessibleName, 'x6-node', () => graph.select(node));
}

export function applyStateMachineEdgeAccessibility(graph: Graph, edge: Edge) {
    const data = edge.getData() as { accessibleName?: unknown; name?: unknown } | undefined;
    const accessibleName = typeof data?.accessibleName === 'string' && data.accessibleName.trim()
        ? data.accessibleName
        : `${display(data?.name, 'Unnamed transition')}, transition from ${getEndpointName(graph, edge.getSourceCellId())} to ${getEndpointName(graph, edge.getTargetCellId())}`;
    applyStateMachineCellAccessibility(graph, edge, accessibleName, 'x6-edge', () => graph.select(edge));
}

function applyStateMachineCellAccessibility(graph: Graph, cell: Node | Edge, accessibleName: string, className: string, selectCell: () => void) {
    const view = graph.findViewByCell(cell);
    const container = view?.container
        ?? graph.container.querySelector<SVGGElement>(`.${className}[data-cell-id="${CSS.escape(cell.id)}"]`);
    if (!container || !accessibleName)
        return;

    container.setAttribute('role', 'button');
    container.setAttribute('aria-label', accessibleName);
    container.setAttribute('tabindex', '0');
    container.setAttribute('focusable', 'true');

    if (container.getAttribute(AccessibilityMarker) === 'true')
        return;

    container.setAttribute(AccessibilityMarker, 'true');
    container.addEventListener('keydown', event => {
        const keyboardEvent = event as KeyboardEvent;
        if (keyboardEvent.key !== 'Enter' && keyboardEvent.key !== ' ')
            return;

        keyboardEvent.preventDefault();
        selectCell();
    });
}

function getEndpointName(graph: Graph, cellId: string | undefined) {
    if (!cellId)
        return 'Unnamed state';

    const cell = graph.getCellById(cellId);
    const data = cell?.getData() as { name?: unknown } | undefined;
    const title = cell?.attr('title/text');
    return display(data?.name ?? title, 'Unnamed state');
}

function display(value: unknown, fallback: string) {
    return typeof value === 'string' && value.trim() ? value : fallback;
}
