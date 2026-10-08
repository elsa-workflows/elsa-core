// Public surface invoked by the Blazor side via JSInterop. Each named export
// here corresponds to a `module.InvokeVoidAsync("...")` / `InvokeAsync<T>("...")`
// call from `ReactFlowGraphApi.cs` — keeping the names stable matters.
//
// The two non-trivial entry points (createReactGraph because it owns React
// rendering, readReactGraph because it transforms graph state into the X6-cell
// shape C# expects) live in their own files. Everything else is a thin
// forwarder to the binding registered for `graphId`, which is tiny enough to
// keep inline rather than spread across one-line files.

import { reactBindings } from '../bindings';
import type { ElsaActivityNode, ElsaActivityStats, ElsaEdge, ElsaGraph, SequenceLayoutOrientation } from '../types';

export { createReactGraph } from './create-react-graph';
export { readReactGraph } from './read-react-graph';

export function loadReactGraph(graphId: string, data: string | ElsaGraph): void {
    const binding = reactBindings[graphId];
    if (!binding) return;
    const graph = typeof data === 'string' ? (JSON.parse(data) as ElsaGraph) : data;
    binding.setGraph(graph);
}

export function disposeReactGraph(graphId: string): void {
    const binding = reactBindings[graphId];
    if (!binding) return;
    try {
        binding.root.unmount();
    } catch {
        // Ignore unmount errors during page tear-down.
    }
    delete reactBindings[graphId];
}

export function selectReactActivity(graphId: string, activityId: string): void {
    reactBindings[graphId]?.selectNode(activityId);
}

export function zoomReactGraphToFit(graphId: string): void {
    reactBindings[graphId]?.fitView();
}

export function centerReactGraphContent(graphId: string): void {
    reactBindings[graphId]?.centerContent();
}

export function addReactActivityNode(graphId: string, node: ElsaActivityNode): void {
    const binding = reactBindings[graphId];
    if (!binding) return;
    // The wrapper supplies the drop point in page-coordinate space via
    // node.position (see FlowchartDesignerWrapper.AddNewActivityAsync).
    // Translate that into flow coordinates inside the designer.
    const dropPagePosition = node.position
        ? { x: node.position.x, y: node.position.y }
        : undefined;
    binding.addNode(node, dropPagePosition);
}

export function updateReactActivity(graphId: string, node: ElsaActivityNode): void {
    reactBindings[graphId]?.updateNode(node);
}

export function updateReactActivityStats(graphId: string, activityId: string, stats: ElsaActivityStats): void {
    reactBindings[graphId]?.updateNodeStats(activityId, stats);
}

export function autoLayoutReactGraph(graphId: string): void {
    reactBindings[graphId]?.autoLayout();
}

export function setSequenceOrientation(graphId: string, orientation: SequenceLayoutOrientation): void {
    reactBindings[graphId]?.setSequenceOrientation(orientation);
}

export function moveSelectedReactSequenceNode(graphId: string, direction: -1 | 1): void {
    reactBindings[graphId]?.moveSelectedSequenceNode(direction);
}

// Called by .NET after it regenerates IDs server-side
// (ReactFlowDesigner.HandlePasteCellsRequested). Mirrors the X6 path's pasteCells.
export function pasteReactCells(graphId: string, activityNodes: ElsaActivityNode[], edges: ElsaEdge[]): void {
    reactBindings[graphId]?.pasteCells(activityNodes, edges);
}
