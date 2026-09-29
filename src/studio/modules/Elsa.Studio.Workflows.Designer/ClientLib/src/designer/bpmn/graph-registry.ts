/**
 * The live BPMN graphs, keyed by container id.
 *
 * A registry of its own rather than an entry in `api/graph-bindings.ts`: that map's `GraphBinding`
 * carries a `DesignerMode` and a `DotNetFlowchartDesigner`, and a BPMN canvas is neither. Widening
 * `DesignerMode` with a fourth member would oblige every `getDesignerModePolicy` switch in the
 * flowchart designer to answer for a mode it never renders, which is a larger change to working
 * code than the separation costs.
 *
 * The consequence is that anything reached by graph id has to look in both maps; `api/find-graph.ts`
 * is the single place that does.
 */
import type { Graph } from '@antv/x6';
import type { BpmnViewModel } from '../../bpmn';
import type { DotNetBpmnDesigner } from './dotnet-bpmn-designer';

export interface BpmnGraphBinding {
    readonly graphId: string;
    readonly graph: Graph;
    readonly interop: DotNetBpmnDesigner;
    /** The view model the canvas currently draws, or null before the first load. */
    viewModel: BpmnViewModel | null;
    /**
     * Non-zero while the adapter is selecting on .NET's behalf.
     *
     * Without it, `selectBpmnElement` would echo the selection straight back to the component that
     * asked for it, which is how a properties pane ends up fighting the canvas for control of the
     * selection.
     */
    suppressSelectionCallbacks: number;
}

export const bpmnGraphBindings: Record<string, BpmnGraphBinding> = {};

/** Disposes a BPMN graph if the id names one. Returns whether it did. */
export function disposeBpmnGraphBinding(graphId: string): boolean {
    const binding = bpmnGraphBindings[graphId];

    if (binding == null) return false;

    binding.graph.dispose();
    delete bpmnGraphBindings[graphId];

    return true;
}
