import {Model} from '@antv/x6';
import {graphBindings} from "./graph-bindings";
import {arrangeSequenceGraph, normalizeSequenceOrientation, withSuppressedGraphUpdated} from "./sequence-mode";
import {applyStateMachineGraphAccessibility} from '../internal/state-machine-accessibility';
import {whenCanvasHasHeight} from '../internal/canvas-ready';

export function loadGraph(graphId: string, data: string | Model.FromJSONData) {
    const binding = graphBindings[graphId];
    const {graph} = binding;
    const model = typeof data === 'string' ? JSON.parse(data) : data;
    withSuppressedGraphUpdated(binding, () => graph.fromJSON(model));

    if (binding.mode === 'sequence') {
        binding.layoutOrientation = normalizeSequenceOrientation((model as any).layoutOrientation);
        arrangeSequenceGraph(binding);
    }

    whenCanvasHasHeight(graph, () => graphBindings[graphId] === binding).then(ready => {
        if (!ready)
            return;

        if (binding.mode === 'stateMachine')
            applyStateMachineGraphAccessibility(graph);

        graph.centerContent({padding: 20});
    });
}
