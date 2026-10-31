import {graphBindings} from "./graph-bindings";
import {disposeBpmnGraphBinding} from "../bpmn/graph-registry";

export function disposeGraph(graphId: string) {
    const binding = graphBindings[graphId];

    // A BPMN canvas is registered elsewhere; disposing it here as well means a component that calls
    // the generic teardown cannot leak a graph just because it used the other designer.
    if (!binding) {
        disposeBpmnGraphBinding(graphId);
        return;
    }

    binding.graph.dispose();
    delete graphBindings[graphId];
}
