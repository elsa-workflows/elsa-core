/**
 * The thin layer between the pure cell mapping and a live X6 graph: creating the canvas, putting
 * cells on it, keeping the instance overlay current, and raising selection back to .NET.
 *
 * Everything that could be decided without a browser was decided in `cells.ts`, `graph-options.ts`
 * and `stats.ts`. What is left here is the part that genuinely needs a `Graph`, which is why it is
 * short and why the fixture tests do not need to run X6 to gate the mapping.
 */
import { Graph, type Model } from '@antv/x6';
import { Selection } from '@antv/x6-plugin-selection';
import type { BpmnActivityStats, BpmnElementStats, BpmnViewModel } from '../../bpmn';
import type { DotNetComponentRef } from '../api/graph-bindings';
import { whenCanvasHasHeight } from '../internal/canvas-ready';
import {
    buildBpmnX6Cells,
    flowTakenLineAttrs,
    statsBadgeAttrs,
    type BpmnElementCellData,
    type BpmnFlowCellData,
    type BpmnX6Cells,
} from './cells';
import { BPMN_DESIGNER_CLASS } from './constants';
import { DotNetBpmnDesigner, type BpmnElementSelection } from './dotnet-bpmn-designer';
import { bpmnGraphBindings, type BpmnGraphBinding } from './graph-registry';
import { createBpmnGraphOptions, type BpmnGraphSettings } from './graph-options';
import { registerBpmnShapes } from './shapes';
import { resolveBpmnStatsBadge } from './stats';

/**
 * Creates a read-only BPMN canvas in the given container and registers it under the container id.
 *
 * Only the Selection plugin is installed. The clipboard, history, transform, snapline and keyboard
 * plugins the flowchart designer uses all exist to change a diagram, and W9c does not: leaving them
 * off is a stronger guarantee than configuring them not to fire.
 */
export function createBpmnGraph(containerId: string, componentRef: DotNetComponentRef, settings?: BpmnGraphSettings): string {
    const container = document.getElementById(containerId);

    if (container == null) throw new Error(`Cannot create a BPMN graph: no element with id '${containerId}'.`);

    // The class carries the custom properties every shape paints with, so the adapter is themed
    // whatever wrapper it is hosted in.
    container.classList.add(BPMN_DESIGNER_CLASS);
    registerBpmnShapes();

    const interop = new DotNetBpmnDesigner(componentRef);
    const graph = new Graph({ container, ...createBpmnGraphOptions(settings) });

    graph.use(new Selection({
        enabled: true,
        multiple: false,
        movable: false,
        rubberband: false,
        rubberNode: false,
        rubberEdge: false,
        showNodeSelectionBox: true,
        className: 'elsa-bpmn-selection',
    }));

    const binding: BpmnGraphBinding = {
        graphId: containerId,
        graph,
        interop,
        viewModel: null,
        suppressSelectionCallbacks: 0,
    };

    bpmnGraphBindings[containerId] = binding;
    wireEvents(binding);

    return containerId;
}

function wireEvents(binding: BpmnGraphBinding): void {
    const { graph, interop } = binding;

    graph.on('node:click', ({ node }) => {
        const data = elementData(node.getData());

        // A lane or a pool is scenery. Clicking one means "nothing in particular", the same as
        // clicking the canvas, rather than selecting a container the document does not treat as an
        // element. (Containers are also `pointerEvents: 'none'`, so this is belt and braces.)
        if (data == null) {
            graph.cleanSelection();
            void interop.raiseCanvasSelected();
            return;
        }

        if (!graph.isSelected(node)) graph.select(node);
    });

    graph.on('node:selected', ({ node }) => {
        if (binding.suppressSelectionCallbacks > 0) return;

        const data = elementData(node.getData());

        if (data == null) return;

        void interop.raiseElementSelected(toSelection(data));
    });

    graph.on('node:dblclick', ({ node }) => {
        const data = elementData(node.getData());

        if (data == null) return;

        void interop.raiseElementDoubleClick(toSelection(data));
    });

    graph.on('blank:click', () => {
        graph.cleanSelection();
        void interop.raiseCanvasSelected();
    });
}

/**
 * Replaces whatever the canvas holds with the cells for this view model.
 *
 * Reports, loudly, when the canvas ends up holding fewer cells than the mapping produced. X6 keys
 * its model by cell id and a second cell with the same id replaces the first, so `cells.ts` itself
 * never emits two cells that would collide -- a document whose element ids are not unique, which the
 * view model reports as `duplicate-element-id` but still renders, has its duplicate dropped there and
 * named in `cells.collisions`. This still checks the canvas against the mapping and still logs
 * `cells.collisions`, so a divergence this layer did not anticipate is not left to be discovered as a
 * diagram that is quietly missing an element.
 */
export function loadBpmnDiagram(binding: BpmnGraphBinding, viewModel: BpmnViewModel): void {
    const cells = buildBpmnX6Cells(viewModel);

    binding.viewModel = viewModel;
    // `Node.Metadata`/`Edge.Metadata` are what X6 accepts through `addNode`/`addEdge`; `fromJSON`
    // declares the stricter `Cell.Properties`, which the same objects satisfy at run time.
    binding.graph.fromJSON({ cells: [...cells.nodes, ...cells.edges] } as Model.FromJSONData);

    reportDivergence(binding, cells);

    void whenCanvasHasHeight(binding.graph, () => bpmnGraphBindings[binding.graphId] === binding).then(ready => {
        if (!ready) return;

        binding.graph.zoomToFit({ padding: 24, minScale: 0.2, maxScale: 1 });
    });
}

function reportDivergence(binding: BpmnGraphBinding, cells: BpmnX6Cells): void {
    const nodeCount = binding.graph.getNodes().length;
    const edgeCount = binding.graph.getEdges().length;

    if (nodeCount !== cells.nodes.length || edgeCount !== cells.edges.length) {
        console.error(
            `BPMN diagram '${binding.graphId}': the view model maps to ${cells.nodes.length} nodes and ${cells.edges.length} edges, `
            + `but the canvas holds ${nodeCount} and ${edgeCount}. X6 replaces a cell whose id is already taken, so the diagram on `
            + 'screen is missing something the document contains -- most likely two elements sharing an id.');
    }

    for (const undrawn of cells.undrawn) {
        console.warn(`BPMN diagram '${binding.graphId}': the ${undrawn.kind} '${undrawn.id}' is not drawn. ${undrawn.reason}`);
    }

    for (const collision of cells.collisions) {
        console.error(`BPMN diagram '${binding.graphId}': the ${collision.kind} '${collision.id}' is not drawn. ${collision.reason}`);
    }
}

/**
 * Replaces the element-keyed instance overlay wholesale.
 *
 * The map is authoritative: an element the map no longer mentions loses its badge. Merging instead
 * would leave a fault, or a block, on screen after the instance that produced it moved on -- a
 * stale diagram that reads as a live one.
 */
export function updateBpmnElementStats(
    binding: BpmnGraphBinding,
    elementStats: Readonly<Record<string, BpmnElementStats>> | null | undefined): void {
    for (const node of binding.graph.getNodes()) {
        const data = elementData(node.getData());

        if (data == null) continue;

        const stats = elementStats?.[data.elementId] ?? null;

        node.setData({ ...data, stats }, { overwrite: true });
        node.attr(statsBadgeAttrs(resolveBpmnStatsBadge(stats, data.activityStats)));
    }

    for (const edge of binding.graph.getEdges()) {
        const data = flowData(edge.getData());

        if (data == null) continue;

        const stats = elementStats?.[data.id] ?? null;

        edge.setData({ ...data, stats }, { overwrite: true });
        edge.attr({ line: flowTakenLineAttrs(stats) });
    }
}

/** Updates the activity-keyed overlay for one Elsa activity, on every element bound to it. */
export function updateBpmnActivityStats(
    binding: BpmnGraphBinding,
    activityId: string,
    activityStats: BpmnActivityStats | null | undefined): void {
    const resolved = activityStats ?? null;

    for (const node of binding.graph.getNodes()) {
        const data = elementData(node.getData());

        if (data == null || data.activityId !== activityId) continue;

        node.setData({ ...data, activityStats: resolved }, { overwrite: true });
        node.attr(statsBadgeAttrs(resolveBpmnStatsBadge(data.stats, resolved)));
    }
}

/**
 * Selects one element on .NET's behalf.
 *
 * The selection callback is suppressed while it happens: the caller already knows what it asked
 * for, and echoing it back is how a properties pane and a canvas end up in a loop.
 */
export function selectBpmnElement(binding: BpmnGraphBinding, elementId: string, center = false): void {
    const cell = binding.graph.getCellById(elementId);

    if (cell == null) {
        console.warn(`BPMN diagram '${binding.graphId}': no cell for element '${elementId}' to select.`);
        return;
    }

    binding.suppressSelectionCallbacks += 1;

    try {
        binding.graph.resetSelection(cell);

        if (center) binding.graph.centerCell(cell);
    } finally {
        binding.suppressSelectionCallbacks -= 1;
    }
}

function elementData(data: unknown): BpmnElementCellData | null {
    return (data as BpmnElementCellData | null)?.cellKind === 'element' ? data as BpmnElementCellData : null;
}

/** Only a sequence flow can be "taken"; an association's id is a synthetic pair, never a document id. */
function flowData(data: unknown): BpmnFlowCellData | null {
    return (data as BpmnFlowCellData | null)?.cellKind === 'flow' ? data as BpmnFlowCellData : null;
}

function toSelection(data: BpmnElementCellData): BpmnElementSelection {
    return {
        elementId: data.elementId,
        elementType: data.elementType,
        kind: data.kind,
        name: data.name,
        activityId: data.activityId,
        bindingState: data.bindingState,
        bindingKind: data.bindingKind,
        scopeId: data.scopeId,
        scopeActivityId: data.scopeActivityId,
        boundaryHostElementId: data.boundaryHostElementId,
        childScopeId: data.childScopeId,
    };
}
