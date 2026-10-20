// SPIKE (#8901): the whole of the Blazor-specific JS. It only forwards the module's events to a DotNetObjectReference;
// every other call (load, save, select, applyOverlays, clearOverlays, dispose) goes from .NET straight to the handle
// through IJSObjectReference.
import { createModeler } from './bpmn/elsa-bpmn-modeler.js';

export function create(canvas, properties, dotNetRef) {
    const handle = createModeler(canvas, { propertiesContainer: properties });
    handle.on('change', payload => dotNetRef.invokeMethodAsync('OnChanged', payload));
    handle.on('selection', payload => dotNetRef.invokeMethodAsync('OnSelectionChanged', payload));
    return handle;
}
