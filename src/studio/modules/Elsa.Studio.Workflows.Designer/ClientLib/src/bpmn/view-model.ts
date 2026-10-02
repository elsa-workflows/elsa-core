/**
 * Builds the canvas-neutral BPMN view model. See ./README.md for the two-input contract.
 *
 * The rule this file works under: it renders what the document says and what the instance overlay
 * reports, and decides nothing. Which flow a gateway takes, whether a join may fire, what a boundary
 * event interrupts -- none of that is here. The one place it *does* form an opinion is geometry, and
 * only when the document supplies none: see ./fallback-layout.ts, which always says so.
 *
 * Nothing here mutates its input and nothing here writes a document back. D4 keeps the whole
 * document on the client; whatever edits it (W11, W14) edits it in place and sends the whole thing.
 */
import { readDiagramInterchange, type BpmnDiagramInterchange, type DiBounds } from './di-reader';
import {
    BOUNDARY_EVENT_ELEMENT_TYPE,
    classifyElementType,
    isUnboundTask,
    requiresWorkBinding,
} from './element-kinds';
import { computeFallbackLayout, layoutKey, type LayoutRect, type LayoutScope } from './fallback-layout';
import type {
    BpmnActivity,
    BpmnActivityDescriptor,
    BpmnBinding,
    BpmnBindingKind,
    BpmnBoundaryAttachment,
    BpmnDiagnostic,
    BpmnDiagnosticCode,
    BpmnDiagnosticSeverity,
    BpmnRect,
    BpmnViewAssociation,
    BpmnViewElement,
    BpmnViewFlow,
    BpmnViewLane,
    BpmnViewModel,
    BpmnViewModelInput,
    BpmnViewPool,
    BpmnViewScope,
} from './model';
import type { BpmnBounds, BpmnElement, BpmnEventDefinition, BpmnExtensions, BpmnProcessDefinition } from './types.generated';

/**
 * The collections the payload always carries, defaulted for the one caller that might not.
 *
 * `buildBpmnViewModel` sits on a JS interop boundary: what arrives is `JSON.parse` output, not a
 * value TypeScript has checked, and W11/W14 edit that document in place before sending it back
 * whole. A newly added element that has not yet grown an `extensions` object should render as an
 * element with nothing on it, not take the designer down inside whichever consumer dereferences it
 * first.
 */
const EMPTY_EXTENSIONS: BpmnExtensions = {
    documentation: [],
    extensionElements: [],
    foreignAttributes: [],
    foreignChildren: [],
};

const EMPTY_EVENT_DEFINITIONS: readonly BpmnEventDefinition[] = [];

/**
 * Reads an `Elsa.BpmnProcess` activity payload, and optionally the BPMN document it was imported
 * from, into one immutable description of the diagram.
 *
 * Never throws on a malformed input. A document Studio cannot render is reported through
 * {@link BpmnViewModel.diagnostics} and produces an empty-but-valid view model, because the caller
 * is a designer component: a diagnostic it can put on screen is worth more than an exception that
 * takes the circuit down.
 */
export function buildBpmnViewModel(input: BpmnViewModelInput): BpmnViewModel {
    const diagnostics: BpmnDiagnostic[] = [];
    const report = (
        code: BpmnDiagnosticCode,
        severity: BpmnDiagnosticSeverity,
        message: string,
        elementId: string | null = null,
        scopeId: string | null = null) => diagnostics.push({ code, severity, message, elementId, scopeId });

    const rootProcess = input.activity?.process;

    if (rootProcess == null || typeof rootProcess.processId !== 'string') {
        report(
            'not-a-bpmn-process',
            'error',
            `The activity '${input.activity?.id ?? '(none)'}' of type '${input.activity?.type ?? '(none)'}' carries no BPMN process, so there is no diagram to show.`);

        return emptyViewModel(diagnostics);
    }

    const scopes = collectScopes(input.activity, rootProcess, report);
    const diagram = readDiagramInterchange(input.sourceXml);

    for (const duplicate of diagram.duplicateShapeIds) {
        report('duplicate-di-shape', 'warning', `More than one BPMNShape draws '${duplicate}'; the first one in the document is used.`, duplicate);
    }

    const descriptorsByType = indexDescriptors(input.activityDescriptors);
    const placements = resolvePlacements(scopes, diagram);
    const elements: BpmnViewElement[] = [];
    const flows: BpmnViewFlow[] = [];
    const associations: BpmnViewAssociation[] = [];
    const lanes: BpmnViewLane[] = [];
    const seenElementIds = new Map<string, string>();
    const seenFlowIds = new Map<string, string>();

    for (const scope of scopes) {
        const scopeElements = scope.definition.elements ?? [];
        const elementsById = new Map(scopeElements.map(element => [element.elementId, element]));

        for (const element of scopeElements) {
            const alreadySeenInScope = seenElementIds.get(element.elementId);

            if (alreadySeenInScope != null) {
                report(
                    'duplicate-element-id',
                    'error',
                    `The element id '${element.elementId}' is used ${alreadySeenInScope === scope.id
                        ? `twice in scope '${scope.id}'`
                        : `both in scope '${alreadySeenInScope}' and in scope '${scope.id}'`}. Anything keyed by element id -- instance state above all -- cannot tell the two apart.`,
                    element.elementId,
                    scope.id);
            } else {
                seenElementIds.set(element.elementId, scope.id);
            }

            elements.push(buildElement(element, scope, elementsById, placements, diagram, descriptorsByType, input, report));

            if (element.compensationHandlerElementId != null) {
                const targetResolved = elementsById.has(element.compensationHandlerElementId);

                if (!targetResolved) {
                    report(
                        'unresolved-compensation-handler',
                        'error',
                        `The compensation boundary event '${element.elementId}' names the handler '${element.compensationHandlerElementId}', which is not in scope '${scope.id}'.`,
                        element.elementId,
                        scope.id);
                }

                associations.push({
                    sourceElementId: element.elementId,
                    targetElementId: element.compensationHandlerElementId,
                    scopeId: scope.id,
                    targetResolved,
                });
            }
        }

        for (const flow of scope.definition.sequenceFlows ?? []) {
            const alreadySeenFlow = seenFlowIds.get(flow.flowId);

            if (alreadySeenFlow != null) {
                report(
                    'duplicate-flow-id',
                    'error',
                    `The sequence flow id '${flow.flowId}' is used ${alreadySeenFlow === scope.id
                        ? `twice in scope '${scope.id}'`
                        : `both in scope '${alreadySeenFlow}' and in scope '${scope.id}'`}. Anything keyed by flow id cannot tell the two apart.`,
                    flow.flowId,
                    scope.id);
            } else {
                seenFlowIds.set(flow.flowId, scope.id);
            }

            const source = elementsById.get(flow.sourceRef);
            const target = elementsById.get(flow.targetRef);

            if (source == null || target == null) {
                report(
                    'dangling-flow',
                    'error',
                    `The sequence flow '${flow.flowId}' of scope '${scope.id}' runs from '${flow.sourceRef}' to '${flow.targetRef}', and ${source == null ? `'${flow.sourceRef}'` : `'${flow.targetRef}'`} is not an element of that scope. The flow is not drawn.`,
                    flow.flowId,
                    scope.id);

                continue;
            }

            const edge = diagram.edges.get(flow.flowId);

            flows.push({
                id: flow.flowId,
                sourceElementId: flow.sourceRef,
                targetElementId: flow.targetRef,
                name: flow.name ?? null,
                scopeId: scope.id,
                conditionOutcome: flow.conditionOutcome ?? null,
                // The reader records a gateway's default flow on the gateway, and the flag on the flow
                // is only set by documents that say it there instead; honour both so neither
                // representation of the same fact goes missing.
                isDefault: flow.isDefault === true || source.defaultFlowId === flow.flowId,
                stats: input.elementStats?.[flow.flowId] ?? null,
                waypoints: edge?.waypoints ?? [],
                geometrySource: edge != null && edge.waypoints.length > 0 ? 'document' : 'fallback',
                labelGeometry: toBounds(edge?.labelBounds ?? null),
                extensions: flow.extensions ?? EMPTY_EXTENSIONS,
            });
        }

        for (const lane of scope.definition.lanes ?? []) {
            const shape = diagram.shapes.get(lane.laneId);

            lanes.push({
                id: lane.laneId,
                name: lane.name ?? null,
                scopeId: scope.id,
                poolId: lane.poolId ?? null,
                geometry: shape == null ? null : { ...shape.bounds, source: 'document' },
                isHorizontal: shape?.isHorizontal ?? null,
                extensions: lane.extensions ?? EMPTY_EXTENSIONS,
            });
        }
    }

    const pools = buildPools(lanes, diagram, report);
    const usesDocumentGeometry = elements.some(element => element.geometry.source === 'document');
    const reason = fallbackReason(input, diagram);

    // One decision, made where the answer is finally known, so the diagnostic a user reads and the
    // `layout.reason` an adapter surfaces can never say different things. A parse failure is
    // reported even for a process with nothing in it, because it is a fault in the input rather
    // than a description of the diagram; the other two are not, since "nothing was placed" is not
    // news about a process with nothing to place.
    if (diagram.parseError != null) {
        report('source-xml-parse-error', 'error', reason);
    } else if (!usesDocumentGeometry && elements.length > 0) {
        report(
            input.sourceXml == null || input.sourceXml.trim().length === 0 ? 'missing-source-xml' : 'missing-diagram',
            'warning',
            reason);
    }

    for (const element of elements) {
        if (element.geometry.source === 'document' || !usesDocumentGeometry) continue;

        report(
            'missing-shape',
            'warning',
            `The BPMN source carries a diagram but no BPMNShape for '${element.id}', so that element alone is placed by fallback and may overlap the rest.`,
            element.id,
            element.scopeId);
    }

    reportUnresolvedDiagramReferences(diagram, elements, lanes, pools, flows, report);

    return {
        processId: rootProcess.processId,
        name: rootProcess.name ?? null,
        isExecutable: rootProcess.isExecutable === true,
        scopes: scopes.map(scope => scope.view),
        elements,
        flows,
        associations,
        lanes,
        pools,
        layout: usesDocumentGeometry
            ? { source: 'document', reason: null }
            : { source: 'fallback', reason },
        diagnostics,
    };
}

type Report = (
    code: BpmnDiagnosticCode,
    severity: BpmnDiagnosticSeverity,
    message: string,
    elementId?: string | null,
    scopeId?: string | null) => void;

interface ScopeNode {
    readonly id: string;
    readonly definition: BpmnProcessDefinition;
    readonly workBindings: Readonly<Record<string, string>>;
    readonly activitiesById: ReadonlyMap<string, BpmnActivity>;
    /** The process id of the scope a given element hosts, for the elements that host one. */
    readonly childScopeByElementId: ReadonlyMap<string, string>;
    readonly view: BpmnViewScope;
}

/**
 * Walks the activity tree into a flat, depth-first list of scopes.
 *
 * A nested BPMN scope is an `Elsa.BpmnProcess` bound as its host element's work, so the scope
 * hierarchy is exactly the activity hierarchy. `visited` closes the one way that could not
 * terminate: an activity graph that -- through a hand-edited payload -- reaches the very same
 * payload object twice. A *different* payload object that happens to carry a processId already
 * seen is not a cycle but a malformed document -- two distinct nested processes cannot share an
 * id, since everything keyed by it (this map, instance state) could only ever mean one of them --
 * so that case is reported and stopped, rather than silently treated the same as a cycle.
 */
function collectScopes(rootActivity: BpmnActivity, rootProcess: BpmnProcessDefinition, report: Report): ScopeNode[] {
    const scopes: ScopeNode[] = [];
    const visited = new Map<string, { readonly definition: BpmnProcessDefinition; readonly activityId: string }>();

    const walk = (
        activity: BpmnActivity,
        definition: BpmnProcessDefinition,
        parentScopeId: string | null,
        hostElementId: string | null,
        depth: number): void => {
        const seen = visited.get(definition.processId);

        if (seen != null) {
            if (seen.definition !== definition) {
                report(
                    'duplicate-process-id',
                    'error',
                    `The processId '${definition.processId}' is used by two different nested BPMN processes, hosted by the activities '${seen.activityId}' and '${activity.id}'. Instance state and the scope hierarchy cannot tell the two apart, so only the first is shown.`,
                    hostElementId,
                    parentScopeId);
            }

            return;
        }

        visited.set(definition.processId, { definition, activityId: activity.id });

        const workBindings = activity.workBindings ?? {};
        const activitiesById = new Map((activity.activities ?? []).map(child => [child.id, child]));
        const childScopeByElementId = new Map<string, string>();
        const nested: { element: BpmnElement; activity: BpmnActivity; process: BpmnProcessDefinition }[] = [];

        for (const element of definition.elements ?? []) {
            const boundActivityId = element.bindingRef == null ? null : workBindings[element.bindingRef] ?? null;
            const bound = boundActivityId == null ? null : activitiesById.get(boundActivityId) ?? null;

            if (bound?.process == null || typeof bound.process.processId !== 'string') continue;

            childScopeByElementId.set(element.elementId, bound.process.processId);
            nested.push({ element, activity: bound, process: bound.process });
        }

        scopes.push({
            id: definition.processId,
            definition,
            workBindings,
            activitiesById,
            childScopeByElementId,
            view: {
                id: definition.processId,
                name: definition.name ?? null,
                isExecutable: definition.isExecutable === true,
                isTransaction: definition.isTransaction === true,
                parentScopeId,
                hostElementId,
                activityId: activity.id,
                depth,
            },
        });

        for (const child of nested) {
            walk(child.activity, child.process, definition.processId, child.element.elementId, depth + 1);
        }
    };

    walk(rootActivity, rootProcess, null, null, 0);

    return scopes;
}

/**
 * Resolves every element to a rectangle, preferring the document's own DI shape and falling back to
 * a computed placement only for the elements no shape covers.
 *
 * The fallback is computed for the whole document as soon as *any* element needs it, so that a
 * document with partial DI still places its unplaced elements relative to each other rather than
 * one at a time on top of one another. Those elements are individually reported as `missing-shape`.
 */
function resolvePlacements(scopes: readonly ScopeNode[], diagram: BpmnDiagramInterchange): Map<string, BpmnRect> {
    const placements = new Map<string, BpmnRect>();
    const missing: { scopeId: string; elementId: string }[] = [];

    for (const scope of scopes) {
        for (const element of scope.definition.elements ?? []) {
            const shape = diagram.shapes.get(element.elementId);

            if (shape == null) missing.push({ scopeId: scope.id, elementId: element.elementId });
            else placements.set(layoutKey(scope.id, element.elementId), { ...shape.bounds, source: 'document' });
        }
    }

    if (missing.length === 0) return placements;

    const fallback = computeFallbackLayout(scopes.map(toLayoutScope), scopes[0].id);

    for (const { scopeId, elementId } of missing) {
        const key = layoutKey(scopeId, elementId);
        const rect: LayoutRect = fallback.get(key) ?? { x: 0, y: 0, width: 100, height: 80 };

        placements.set(key, { ...rect, source: 'fallback' });
    }

    return placements;
}

function toLayoutScope(scope: ScopeNode): LayoutScope {
    return {
        id: scope.id,
        elements: (scope.definition.elements ?? []).map(element => ({
            id: element.elementId,
            elementType: element.elementType,
            attachedToElementId: element.attachedToRef ?? null,
            childScopeId: scope.childScopeByElementId.get(element.elementId) ?? null,
        })),
        flows: (scope.definition.sequenceFlows ?? []).map(flow => ({ sourceRef: flow.sourceRef, targetRef: flow.targetRef })),
    };
}

function buildElement(
    element: BpmnElement,
    scope: ScopeNode,
    elementsById: ReadonlyMap<string, BpmnElement>,
    placements: ReadonlyMap<string, BpmnRect>,
    diagram: BpmnDiagramInterchange,
    descriptorsByType: ReadonlyMap<string, BpmnActivityDescriptor>,
    input: BpmnViewModelInput,
    report: Report): BpmnViewElement {
    const shape = diagram.shapes.get(element.elementId);
    const binding = resolveBinding(element.bindingRef ?? null, element, scope, descriptorsByType);
    const listenerBinding = element.listenerBindingRef == null
        ? null
        : resolveBinding(element.listenerBindingRef, element, scope, descriptorsByType);

    if (binding?.state === 'unbound') {
        report(
            'unbound-work',
            'warning',
            `The ${element.elementType} '${element.name ?? element.elementId}' says what it is for but not how to perform it, and nothing binds it to an Elsa activity. The workflow cannot be published until it does.`,
            element.elementId,
            scope.id);
    } else if (binding?.state === 'unresolved') {
        report(
            'unresolved-binding',
            'error',
            `The ${element.elementType} '${element.name ?? element.elementId}' declares the binding ref '${element.bindingRef}', which scope '${scope.id}' does not map to an activity it carries.`,
            element.elementId,
            scope.id);
    }

    if (listenerBinding?.state === 'unresolved') {
        report(
            'unresolved-listener-binding',
            'error',
            `The event subprocess '${element.name ?? element.elementId}' declares the listener binding ref '${element.listenerBindingRef}', which scope '${scope.id}' does not map to an activity it carries.`,
            element.elementId,
            scope.id);
    }

    let boundary: BpmnBoundaryAttachment | null = null;

    if (element.elementType === BOUNDARY_EVENT_ELEMENT_TYPE && element.attachedToRef != null) {
        const hostResolved = elementsById.has(element.attachedToRef);

        if (!hostResolved) {
            report(
                'unresolved-boundary-host',
                'error',
                `The boundary event '${element.elementId}' is attached to '${element.attachedToRef}', which is not an element of scope '${scope.id}'.`,
                element.elementId,
                scope.id);
        }

        boundary = { hostElementId: element.attachedToRef, hostResolved, interrupting: element.cancelActivity === true };
    }

    const activityId = binding?.activityId ?? null;

    return {
        id: element.elementId,
        elementType: element.elementType,
        kind: classifyElementType(element.elementType),
        name: element.name ?? null,
        scopeId: scope.id,
        parentElementId: scope.view.hostElementId,
        childScopeId: scope.childScopeByElementId.get(element.elementId) ?? null,
        laneId: element.laneId ?? null,
        geometry: placements.get(layoutKey(scope.id, element.elementId))
            ?? { x: 0, y: 0, width: 100, height: 80, source: 'fallback' },
        labelGeometry: toBounds(shape?.labelBounds ?? null),
        binding,
        listenerBinding,
        stats: input.elementStats?.[element.elementId] ?? null,
        activityStats: activityId == null ? null : input.activityStats?.[activityId] ?? null,
        boundary,
        eventDefinitions: element.eventDefinitions ?? EMPTY_EVENT_DEFINITIONS,
        loopCharacteristics: element.loopCharacteristics ?? null,
        isForCompensation: element.isForCompensation === true,
        isTransaction: element.isTransaction === true,
        isEventSubProcess: element.triggeredByEvent === true,
        defaultFlowId: element.defaultFlowId ?? null,
        isExpanded: shape?.isExpanded ?? null,
        isMarkerVisible: shape?.isMarkerVisible ?? null,
        properties: element.properties ?? {},
        extensions: element.extensions ?? EMPTY_EXTENSIONS,
    };
}

/**
 * Resolves one binding ref through `workBindings` and the scope's own activities.
 *
 * Returns null only for an element that declares no binding ref *and* needs none. An element that
 * needs work performed and declares none is reported as `'unbound'` rather than as nothing: W8
 * makes that a publish error, so it has to be a state the canvas can show, not an absence.
 */
function resolveBinding(
    bindingRef: string | null,
    element: BpmnElement,
    scope: ScopeNode,
    descriptorsByType: ReadonlyMap<string, BpmnActivityDescriptor>): BpmnBinding | null {
    const kind: BpmnBindingKind = isUnboundTask(element.elementType, element.properties) ? 'unboundTask' : 'automatic';

    if (bindingRef == null) {
        if (!requiresWorkBinding(element.elementType)) return null;

        return {
            state: 'unbound',
            kind,
            bindingRef: null,
            activityId: null,
            activityType: null,
            activityName: null,
            displayName: null,
            descriptor: null,
        };
    }

    const activityId = scope.workBindings[bindingRef] ?? null;
    const activity = activityId == null ? null : scope.activitiesById.get(activityId) ?? null;

    if (activity == null) {
        return {
            state: 'unresolved',
            kind,
            bindingRef,
            activityId,
            activityType: null,
            activityName: null,
            displayName: null,
            descriptor: null,
        };
    }

    const descriptor = descriptorsByType.get(activity.type) ?? null;
    const activityName = emptyToNull(activity.name);

    return {
        state: 'bound',
        kind,
        bindingRef,
        activityId,
        activityType: activity.type,
        activityName,
        displayName: activityName ?? emptyToNull(descriptor?.displayName) ?? activity.type,
        descriptor,
    };
}

/**
 * The pools a lane can belong to, from the source document's collaboration.
 *
 * A pool is not on the activity payload -- its root is a single process -- so a document Studio has
 * no source XML for has no pool names. A lane that names a pool nothing declares still gets one
 * here, unnamed and ungeometried, so the lane has somewhere to hang: dropping it would make the
 * lane look like it belongs to the process itself, which is a different picture.
 */
function buildPools(
    lanes: readonly BpmnViewLane[],
    diagram: BpmnDiagramInterchange,
    report: Report): BpmnViewPool[] {
    const pools: BpmnViewPool[] = [];
    const declared = new Set<string>();

    for (const participant of diagram.participants) {
        const shape = diagram.shapes.get(participant.id);

        declared.add(participant.id);
        pools.push({
            id: participant.id,
            name: participant.name,
            processId: participant.processId,
            geometry: shape == null ? null : { ...shape.bounds, source: 'document' },
            isHorizontal: shape?.isHorizontal ?? null,
        });
    }

    for (const lane of lanes) {
        if (lane.poolId == null || declared.has(lane.poolId)) continue;

        declared.add(lane.poolId);
        pools.push({ id: lane.poolId, name: null, processId: null, geometry: null, isHorizontal: null });
        report(
            'unresolved-pool',
            'info',
            `The lane '${lane.id}' belongs to the pool '${lane.poolId}', which the stored BPMN source does not declare as a participant, so the pool is drawn without a name.`,
            lane.id,
            lane.scopeId);
    }

    return pools;
}

/**
 * Reports DI that draws something this document does not have.
 *
 * A BPMN document legitimately carries DI for things the payload does not model -- text
 * annotations, data objects, and the association edge between a compensation boundary event and its
 * handler, which the payload records as a property rather than as an element with an id. So an
 * unresolved shape or edge is reported as information, not as a defect: the point is that the
 * fixture tests can assert every shape resolves for the documents where it should, and that a
 * reader change which starts silently dropping elements shows up here rather than nowhere.
 */
function reportUnresolvedDiagramReferences(
    diagram: BpmnDiagramInterchange,
    elements: readonly BpmnViewElement[],
    lanes: readonly BpmnViewLane[],
    pools: readonly BpmnViewPool[],
    flows: readonly BpmnViewFlow[],
    report: Report): void {
    const drawable = new Set<string>([
        ...elements.map(element => element.id),
        ...lanes.map(lane => lane.id),
        ...pools.map(pool => pool.id),
    ]);
    const drawableFlows = new Set(flows.map(flow => flow.id));

    for (const shape of diagram.shapes.values()) {
        if (drawable.has(shape.elementId)) continue;

        report('unresolved-di-shape', 'info', `The BPMN source draws a shape for '${shape.elementId}', which is not an element, lane or pool of this process.`, shape.elementId);
    }

    for (const edge of diagram.edges.values()) {
        if (drawableFlows.has(edge.elementId)) continue;

        report('unresolved-di-edge', 'info', `The BPMN source draws an edge for '${edge.elementId}', which is not a sequence flow of this process.`, edge.elementId);
    }
}

/** Why a diagram would be laid out by fallback -- used both as `layout.reason` and as a diagnostic. */
function fallbackReason(input: BpmnViewModelInput, diagram: BpmnDiagramInterchange): string {
    const consequence = 'so the diagram is laid out by fallback rather than by the coordinates its author chose.';

    if (diagram.parseError != null) return `The stored BPMN source could not be parsed as XML (${diagram.parseError}), ${consequence}`;
    if (input.sourceXml == null || input.sourceXml.trim().length === 0) return `This workflow definition carries no BPMN source document, ${consequence}`;
    if (!diagram.hasDiagram) return `The stored BPMN source carries no BPMNDI diagram section, ${consequence}`;

    return `The stored BPMN source carries a diagram, but no BPMNShape for any element of this process, ${consequence}`;
}

function indexDescriptors(
    descriptors: readonly BpmnActivityDescriptor[] | null | undefined): ReadonlyMap<string, BpmnActivityDescriptor> {
    const byType = new Map<string, BpmnActivityDescriptor>();

    for (const descriptor of descriptors ?? []) {
        if (descriptor?.typeName == null) continue;

        const existing = byType.get(descriptor.typeName);

        // The catalogue can carry several versions of one activity type; the highest wins, which is
        // the one the designer would place.
        if (existing == null || (descriptor.version ?? 0) >= (existing.version ?? 0)) byType.set(descriptor.typeName, descriptor);
    }

    return byType;
}

function toBounds(bounds: DiBounds | null): Readonly<BpmnBounds> | null {
    return bounds == null ? null : { x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height };
}

function emptyToNull(value: string | null | undefined): string | null {
    return value == null || value.trim().length === 0 ? null : value;
}

function emptyViewModel(diagnostics: readonly BpmnDiagnostic[]): BpmnViewModel {
    return {
        processId: '',
        name: null,
        isExecutable: false,
        scopes: [],
        elements: [],
        flows: [],
        associations: [],
        lanes: [],
        pools: [],
        layout: { source: 'fallback', reason: 'There is no BPMN process to lay out.' },
        diagnostics,
    };
}
