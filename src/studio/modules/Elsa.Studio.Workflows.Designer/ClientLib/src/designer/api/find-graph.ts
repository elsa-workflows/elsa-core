import type {Graph} from '@antv/x6';
import {graphBindings} from './graph-bindings';
import {bpmnGraphBindings} from '../bpmn/graph-registry';

/**
 * Resolves a graph by container id, whichever designer created it.
 *
 * The flowchart, sequence and state-machine designers share one binding registry; the BPMN adapter
 * keeps its own, because a BPMN canvas has neither a `DesignerMode` nor a
 * `DotNetFlowchartDesigner`. The viewport commands -- zoom to fit, centre -- are the same operation
 * on either, so they go through here rather than growing a BPMN-specific twin.
 *
 * Returns null rather than throwing for an id that names nothing: these are called from .NET after
 * a component has already begun disposing often enough that a missing graph is a normal race, not a
 * defect.
 */
export function findGraph(graphId: string): Graph | null {
    return graphBindings[graphId]?.graph ?? bpmnGraphBindings[graphId]?.graph ?? null;
}
