import {graphBindings} from './graph-bindings';

export function selectCell(graphId: string, cellId: string, center = false) {
    const binding = graphBindings[graphId];
    const graph = binding?.graph;
    const cell = graph?.getCellById(cellId);
    if (!binding || !graph || !cell)
        return;

    binding.suppressSelectionCallbacks = (binding.suppressSelectionCallbacks ?? 0) + 1;
    try {
        graph.resetSelection(cell);
        if (center)
            graph.centerCell(cell);
    } finally {
        binding.suppressSelectionCallbacks--;
    }
}
