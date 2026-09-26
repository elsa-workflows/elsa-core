import {graphBindings} from './graph-bindings';
import {moveSelectedSequenceNode, setSequenceOrientation} from './sequence-mode';

export async function setX6SequenceOrientation(graphId: string, orientation: string) {
    const binding = graphBindings[graphId];
    if (!binding || binding.mode !== 'sequence') return;

    setSequenceOrientation(binding, orientation);
    await binding.interop.raiseGraphUpdated();
}

export async function moveSelectedX6SequenceNode(graphId: string, direction: number) {
    const binding = graphBindings[graphId];
    if (!binding || binding.mode !== 'sequence') return;

    if (moveSelectedSequenceNode(binding, direction))
        await binding.interop.raiseGraphUpdated();
}
