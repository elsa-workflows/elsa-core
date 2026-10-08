/**
 * The JS interop surface of the BPMN canvas: the functions W10's Blazor component calls.
 *
 * Every one of them takes the container id as its first argument, the same convention the flowchart
 * designer's API uses, so a `X6GraphApi`-shaped wrapper on the .NET side works unchanged. The
 * viewport commands are deliberately *not* duplicated here: `zoomToFit` and `centerContent` already
 * resolve a graph from either registry, so a BPMN canvas uses the same two functions everything
 * else does.
 */
import type {BpmnDiagnostic, BpmnViewModel, BpmnViewModelInput, BpmnActivityStats, BpmnElementStats} from '../../bpmn';
import {buildBpmnViewModel} from '../../bpmn';
import {bpmnGraphBindings, disposeBpmnGraphBinding} from '../bpmn/graph-registry';
import type {BpmnGraphBinding} from '../bpmn/graph-registry';
import {
    createBpmnGraph as mountBpmnGraph,
    loadBpmnDiagram as loadIntoBinding,
    selectBpmnElement as selectInBinding,
    updateBpmnActivityStats as updateActivityStatsInBinding,
    updateBpmnElementStats as updateElementStatsInBinding,
} from '../bpmn/mount';
import type {BpmnGraphSettings} from '../bpmn/graph-options';
import type {DotNetComponentRef} from './graph-bindings';

export type {BpmnGraphSettings} from '../bpmn/graph-options';
export type {BpmnElementSelection} from '../bpmn/dotnet-bpmn-designer';

/**
 * Looks up the BPMN graph binding registered under `graphId`, warning to the console if `action`
 * is given and there is none.
 *
 * Every interop function below needs this same "find it or bail" guard before it can touch a
 * binding; centralising it keeps the four call sites down to the one line that differs between
 * them, without changing which of them logs a warning.
 */
function resolveBpmnBinding(graphId: string, action?: string): BpmnGraphBinding | undefined {
    const binding = bpmnGraphBindings[graphId];

    if (binding == null) {
        if (action != null) console.warn(`No BPMN graph with id '${graphId}' to ${action}.`);
        return undefined;
    }

    return binding;
}

/**
 * Creates a read-only BPMN canvas in the element with the given id and registers it under that id.
 *
 * Selection and double-click on an element are raised to `componentRef` as `HandleActivitySelected`
 * and `HandleActivityDoubleClick` -- the flowchart designer's own method names -- carrying a
 * {@link BpmnElementSelection} rather than an activity. Clicking the canvas, a lane or a pool
 * raises `HandleCanvasSelected`.
 *
 * @returns the graph id, which is the container id.
 */
export function createBpmnGraph(containerId: string, componentRef: DotNetComponentRef, settings?: BpmnGraphSettings): string {
    return mountBpmnGraph(containerId, componentRef, settings);
}

/**
 * Builds the canvas-neutral view model from an `Elsa.BpmnProcess` activity payload and, optionally,
 * the BPMN document it was imported from.
 *
 * Exposed separately from {@link loadBpmnDiagram} because the view model is worth having on the
 * .NET side in its own right: `diagnostics` is what a document with an unbound task, a dangling
 * flow or no BPMN DI at all has to say for itself, and `layout.source` tells the user whether the
 * diagram they are looking at is the author's or Studio's.
 */
export function buildBpmnDiagram(input: BpmnViewModelInput | string): BpmnViewModel {
    return buildBpmnViewModel(typeof input === 'string' ? JSON.parse(input) as BpmnViewModelInput : input);
}

/**
 * Draws a diagram, replacing whatever the canvas held.
 *
 * Accepts either an already-built {@link BpmnViewModel} or the {@link BpmnViewModelInput} to build
 * one from -- the two are told apart by `elements`, which only the view model has -- so a caller
 * that has no use for the diagnostics can pass the activity payload straight across interop instead
 * of sending a whole view model back and forth.
 *
 * @returns the view model's diagnostics, in document order.
 */
export function loadBpmnDiagram(graphId: string, model: BpmnViewModel | BpmnViewModelInput | string): readonly BpmnDiagnostic[] {
    const binding = resolveBpmnBinding(graphId, 'load a diagram into');

    if (binding == null) return [];

    const parsed = typeof model === 'string' ? JSON.parse(model) : model;
    const viewModel: BpmnViewModel = Array.isArray((parsed as BpmnViewModel).elements)
        ? parsed as BpmnViewModel
        : buildBpmnViewModel(parsed as BpmnViewModelInput);

    loadIntoBinding(binding, viewModel);

    return viewModel.diagnostics;
}

/**
 * Replaces the whole element-keyed instance overlay.
 *
 * The map is authoritative: an element it no longer mentions loses its badge, so a fault that has
 * been cleared disappears from the canvas rather than outliving the instance that raised it. Pass
 * null or an empty map to clear the overlay entirely.
 */
export function updateBpmnElementStats(graphId: string, elementStats: Readonly<Record<string, BpmnElementStats>> | null): void {
    const binding = resolveBpmnBinding(graphId);

    if (binding == null) return;

    updateElementStatsInBinding(binding, elementStats);
}

/**
 * Updates the activity-keyed stats for one Elsa activity, on every element bound to it.
 *
 * This is the existing `ActivityStats` the workflow instance viewer already fetches; the
 * element-keyed map is the one that lets a gateway or an event -- which are not bound work and have
 * no activity id -- light up at all.
 */
export function updateBpmnActivityStats(graphId: string, activityId: string, activityStats: BpmnActivityStats | null): void {
    const binding = resolveBpmnBinding(graphId);

    if (binding == null) return;

    updateActivityStatsInBinding(binding, activityId, activityStats);
}

/** Selects one BPMN element by its element id, optionally centring the viewport on it. */
export function selectBpmnElement(graphId: string, elementId: string, center = false): void {
    const binding = resolveBpmnBinding(graphId);

    if (binding == null) return;

    selectInBinding(binding, elementId, center);
}

/** Disposes the BPMN canvas registered under this id. Safe to call for an id that names none. */
export function disposeBpmnGraph(graphId: string): void {
    disposeBpmnGraphBinding(graphId);
}
