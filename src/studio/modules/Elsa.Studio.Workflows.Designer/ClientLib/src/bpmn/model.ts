/**
 * The canvas-neutral BPMN view model: everything that is true about a BPMN diagram regardless of
 * who draws it. See ./README.md for the two-input contract and the rules this module works under.
 *
 * Everything here is plain, immutable data with no methods, so an X6 adapter, a React Flow adapter
 * and .NET JS interop can all consume the same value without adapting it first.
 */
import type {
    BpmnBounds,
    BpmnEventDefinition,
    BpmnExtensions,
    BpmnLoopCharacteristics,
    BpmnPoint,
    BpmnProcessDefinition,
} from './types.generated';

// ---------------------------------------------------------------------------------------------
// Inputs
// ---------------------------------------------------------------------------------------------

/**
 * The part of an Elsa activity's serialized JSON this module reads.
 *
 * An `Elsa.BpmnProcess` activity additionally carries {@link process}, {@link workBindings} and
 * {@link activities}; every other activity is only ever reached as the target of a work binding,
 * where its id, type and name are all this module needs. A nested BPMN scope -- an embedded
 * subprocess or an event subprocess body -- is itself an `Elsa.BpmnProcess` in {@link activities},
 * which is why this one type describes both.
 */
export interface BpmnActivity {
    readonly id: string;
    readonly type: string;
    readonly name?: string | null;
    readonly version?: number;
    /** Present when this activity is a BPMN scope. */
    readonly process?: BpmnProcessDefinition | null;
    /** Maps each binding ref the scope's definition declares to the id of the activity that runs it. */
    readonly workBindings?: Readonly<Record<string, string>> | null;
    /** The activities bound as this scope's work. */
    readonly activities?: readonly BpmnActivity[] | null;
}

/**
 * The activity-descriptor fields needed to name a bound activity.
 *
 * Deliberately a structural subset of `Elsa.Studio.Workflows.Designer.Models.ActivityDescriptorDto`
 * (and of the wider `ActivityDescriptor` the API client exposes) rather than an import of either:
 * nothing in this module may depend on a canvas package or on the designer's own models.
 */
export interface BpmnActivityDescriptor {
    readonly typeName: string;
    readonly version?: number;
    readonly name?: string | null;
    readonly displayName?: string | null;
    readonly category?: string | null;
    readonly description?: string | null;
    readonly color?: string | null;
    readonly icon?: string | null;
}

/**
 * Execution counters for one Elsa activity, keyed by activity id.
 *
 * Mirrors `Elsa.Studio.Workflows.Domain.Models.ActivityStats`, which is what the workflow instance
 * viewer already fetches. Only *bound* work has one of these, which is exactly why
 * {@link BpmnElementStats} exists alongside it.
 */
export interface BpmnActivityStats {
    readonly started?: number;
    readonly completed?: number;
    readonly uncompleted?: number;
    readonly blocked?: boolean;
    readonly faulted?: boolean;
    readonly metadata?: Readonly<Record<string, unknown>> | null;
}

/**
 * Instance state for one BPMN element, keyed by BPMN element id.
 *
 * This module never reads a field of this type -- it only keys the map onto elements -- so the
 * projection that fills it (W13) owns the shape and may widen it without changing anything here.
 * The fields below are the ones a BPMN overlay needs at minimum; all are optional so that a
 * projection which cannot compute one says nothing rather than reporting a zero.
 */
export interface BpmnElementStats {
    /** How many tokens have entered this element. */
    readonly started?: number;
    /** How many tokens have left it having completed. */
    readonly completed?: number;
    /** How many tokens are sitting on it right now (a waiting catch event, an armed listener). */
    readonly active?: number;
    /** Whether a token is parked here waiting for something external. */
    readonly blocked?: boolean;
    /** Whether execution faulted at this element. */
    readonly faulted?: boolean;
    /** Whether a token here was cancelled (an interrupted activity, a lost event race). */
    readonly canceled?: boolean;
}

/** Everything {@link buildBpmnViewModel} reads. */
export interface BpmnViewModelInput {
    /** The root `Elsa.BpmnProcess` activity JSON, exactly as the workflow definition carries it. */
    readonly activity: BpmnActivity;
    /**
     * The imported BPMN document, from the workflow definition's `CustomProperties["Bpmn:SourceXml"]`.
     * Read for BPMN DI geometry and collaboration participants only -- never for process structure.
     * Absent or DI-less source produces a deterministic fallback layout and says so.
     */
    readonly sourceXml?: string | null;
    /** Activity descriptors already loaded by the designer, used to name bound activities. */
    readonly activityDescriptors?: readonly BpmnActivityDescriptor[] | null;
    /** Instance state keyed by BPMN element id. */
    readonly elementStats?: Readonly<Record<string, BpmnElementStats>> | null;
    /** Instance state keyed by Elsa activity id, resolved onto elements through `workBindings`. */
    readonly activityStats?: Readonly<Record<string, BpmnActivityStats>> | null;
}

// ---------------------------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------------------------

/** Where a piece of geometry came from. */
export type BpmnGeometrySource = 'document' | 'fallback';

/** A placed rectangle, in the coordinate space of the document's BPMN plane. */
export interface BpmnRect extends Readonly<BpmnBounds> {
    readonly source: BpmnGeometrySource;
}

/** How an element's work is (or is not) bound to an Elsa activity. */
export type BpmnBindingState =
    /** The element declares a binding ref and `workBindings` resolves it to an activity. */
    | 'bound'
    /** The element needs work performed and declares no binding ref at all. A publish error (W8). */
    | 'unbound'
    /** The element declares a binding ref that `workBindings` -- or `activities` -- does not resolve. */
    | 'unresolved';

/** Where the Elsa activity behind an element comes from. */
export type BpmnBindingKind =
    /**
     * Authored: an `elsa:activityBinding` on the element declares the activity and its inputs
     * (`Bpmn.Interchange`'s `BpmnWorkBinding.UnboundTask`). The only kind a user binds by hand.
     */
    | 'unboundTask'
    /**
     * Derived by elsa-core's binder from the document itself: a timer, message or signal wait, a
     * message publish, a call activity or a nested process. An `elsa:activityBinding` on one of these
     * is refused at import, so it is never edited as one.
     */
    | 'automatic';

/** The bound Elsa activity behind one BPMN element, and how it should be named on the canvas. */
export interface BpmnBinding {
    readonly state: BpmnBindingState;
    /** Whether the activity is authored on the element or bound automatically; see {@link BpmnBindingKind}. */
    readonly kind: BpmnBindingKind;
    /** The binding ref the document declares, or null when the element declares none. */
    readonly bindingRef: string | null;
    /** The Elsa activity id `workBindings` maps {@link bindingRef} to. */
    readonly activityId: string | null;
    /** The bound activity's type name, e.g. `Elsa.WriteLine`. */
    readonly activityType: string | null;
    /** The bound activity's own name, when the author gave it one. */
    readonly activityName: string | null;
    /** What to show: the activity's name, else its descriptor's display name, else its type name. */
    readonly displayName: string | null;
    /** The descriptor {@link activityType} resolved to, when one was supplied. */
    readonly descriptor: BpmnActivityDescriptor | null;
}

/** Which family of BPMN element this is. Refine with {@link BpmnViewElement.elementType}. */
export type BpmnElementKind = 'event' | 'gateway' | 'task' | 'callActivity' | 'subProcess' | 'unknown';

/** A boundary event's attachment to the activity it is drawn on. */
export interface BpmnBoundaryAttachment {
    /** The `attachedToRef` the document declares, resolved or not. */
    readonly hostElementId: string;
    /** Whether the host element was actually found in the same scope. */
    readonly hostResolved: boolean;
    /**
     * Whether catching interrupts the host, i.e. `cancelActivity`. Purely what the document says;
     * what interrupting *does* is the engine's business, not this module's.
     */
    readonly interrupting: boolean;
}

/** One BPMN flow element, in the scope it belongs to. */
export interface BpmnViewElement {
    /** The BPMN element id. Unique per document; see the `duplicate-element-id` diagnostic. */
    readonly id: string;
    /** The raw BPMN element type, e.g. `serviceTask`, `eventBasedGateway`, `boundaryEvent`. */
    readonly elementType: string;
    /** The family {@link elementType} belongs to. */
    readonly kind: BpmnElementKind;
    readonly name: string | null;
    /** The process id of the scope this element lives in. */
    readonly scopeId: string;
    /** The subprocess element hosting {@link scopeId} in its parent, or null at the root scope. */
    readonly parentElementId: string | null;
    /** For a subprocess/transaction/event subprocess: the process id of the scope it contains. */
    readonly childScopeId: string | null;
    /** The lane this element was assigned to, or null. */
    readonly laneId: string | null;
    readonly geometry: BpmnRect;
    readonly labelGeometry: Readonly<BpmnBounds> | null;
    /** How this element's work resolves, or null for an element that performs no work. */
    readonly binding: BpmnBinding | null;
    /**
     * The listener an event subprocess arms while its enclosing scope runs (`listenerBindingRef`).
     *
     * Only an event subprocess has one, and the event it waits for is otherwise invisible on the
     * canvas: the start event inside the body carries the event definition but no binding of its
     * own, so this is the only place the armed work appears.
     */
    readonly listenerBinding: BpmnBinding | null;
    /** Instance state keyed by this element's own id. */
    readonly stats: BpmnElementStats | null;
    /** Instance state for the bound activity, resolved through `workBindings`. */
    readonly activityStats: BpmnActivityStats | null;
    /** Set only on a boundary event. */
    readonly boundary: BpmnBoundaryAttachment | null;
    /** The event definitions the document declares, verbatim. */
    readonly eventDefinitions: readonly BpmnEventDefinition[];
    /** Multi-instance markers, or null when the element is not multi-instance. */
    readonly loopCharacteristics: BpmnLoopCharacteristics | null;
    /** Whether this element is a compensation handler (`isForCompensation`). */
    readonly isForCompensation: boolean;
    /** Whether this element is a transaction subprocess. */
    readonly isTransaction: boolean;
    /** Whether this subprocess is an event subprocess (`triggeredByEvent`). */
    readonly isEventSubProcess: boolean;
    /** The sequence flow taken when no conditional flow out of this gateway matches. */
    readonly defaultFlowId: string | null;
    /** Whether the document's DI draws this subprocess expanded. Null when DI says nothing. */
    readonly isExpanded: boolean | null;
    /** Whether the document's DI draws the gateway's own marker. Null when DI says nothing. */
    readonly isMarkerVisible: boolean | null;
    /** Reader-populated element properties, e.g. `bpmn.calledElement`, `bpmn.messageName`. */
    readonly properties: Readonly<Record<string, string>>;
    /** Documentation, extension elements, foreign attributes and children, retained verbatim. */
    readonly extensions: BpmnExtensions;
}

/** One sequence flow. */
export interface BpmnViewFlow {
    readonly id: string;
    readonly sourceElementId: string;
    readonly targetElementId: string;
    readonly name: string | null;
    readonly scopeId: string;
    /** The outcome the source element must produce for this flow to be taken, when conditional. */
    readonly conditionOutcome: string | null;
    /** Whether this is the source gateway's default flow. */
    readonly isDefault: boolean;
    /** Instance state keyed by this flow's own id -- whether, and how often, it has been taken. */
    readonly stats: BpmnElementStats | null;
    /** Empty when the document carries no DI edge for this flow; see {@link geometrySource}. */
    readonly waypoints: readonly BpmnPoint[];
    readonly geometrySource: BpmnGeometrySource;
    readonly labelGeometry: Readonly<BpmnBounds> | null;
    readonly extensions: BpmnExtensions;
}

/**
 * The association drawn from a compensation boundary event to the handler activity it triggers.
 *
 * The payload records this as `compensationHandlerElementId` on the boundary event, not as an
 * element with an id of its own, so there is nothing to look a DI edge up by: an association never
 * carries document waypoints and the adapter routes it. See ./README.md.
 */
export interface BpmnViewAssociation {
    /** The compensation boundary event. */
    readonly sourceElementId: string;
    /** The compensation handler activity. */
    readonly targetElementId: string;
    readonly scopeId: string;
    /** Whether {@link targetElementId} was found in the same scope. */
    readonly targetResolved: boolean;
}

/** One lane, retained and rendered; this module attaches no meaning to lane membership. */
export interface BpmnViewLane {
    readonly id: string;
    readonly name: string | null;
    readonly scopeId: string;
    readonly poolId: string | null;
    readonly geometry: BpmnRect | null;
    /** Whether the document's DI draws the lane horizontally. Null when DI says nothing. */
    readonly isHorizontal: boolean | null;
    readonly extensions: BpmnExtensions;
}

/**
 * One pool (a `participant` on the document's collaboration), retained and rendered.
 *
 * Pools are not on the activity payload at all -- the payload's root is a single process -- so a
 * pool's name and geometry come from the source XML. A pool referenced by a lane in the payload
 * with no participant in the XML is still reported here, unnamed, so the lane has a parent.
 */
export interface BpmnViewPool {
    readonly id: string;
    readonly name: string | null;
    /** The process the participant refers to, when the document says. */
    readonly processId: string | null;
    readonly geometry: BpmnRect | null;
    readonly isHorizontal: boolean | null;
}

/** One BPMN process scope: the root process, or a subprocess/event subprocess body. */
export interface BpmnViewScope {
    /** The process id, which is also {@link BpmnViewElement.scopeId} for its elements. */
    readonly id: string;
    readonly name: string | null;
    readonly isExecutable: boolean;
    readonly isTransaction: boolean;
    /** The enclosing scope's process id, or null for the root scope. */
    readonly parentScopeId: string | null;
    /** The subprocess element in the parent scope that hosts this one, or null for the root. */
    readonly hostElementId: string | null;
    /** The id of the `Elsa.BpmnProcess` activity that runs this scope. */
    readonly activityId: string;
    /** How deep this scope sits; 0 for the root. */
    readonly depth: number;
}

/** What a diagnostic is about. */
export type BpmnDiagnosticCode =
    /** The input activity is not a BPMN scope, so there is nothing to render. */
    | 'not-a-bpmn-process'
    /** No source XML was supplied, so the whole diagram is laid out by fallback. */
    | 'missing-source-xml'
    /** The source XML could not be parsed as XML. */
    | 'source-xml-parse-error'
    /**
     * The source XML carries no BPMN DI this document can be placed from -- either no diagram
     * section at all, or one with no shape for any element of this process -- so the layout is a
     * fallback.
     */
    | 'missing-diagram'
    /** One element has no DI shape; that element alone is placed by fallback. */
    | 'missing-shape'
    /** A DI shape names an element, lane or pool this document does not have. */
    | 'unresolved-di-shape'
    /** A DI edge names a flow this document does not have. */
    | 'unresolved-di-edge'
    /** Two DI shapes claim the same element id; the first is used. */
    | 'duplicate-di-shape'
    /** Two elements in this document share an id. */
    | 'duplicate-element-id'
    /** Two sequence flows in this document share an id. */
    | 'duplicate-flow-id'
    /** An element that needs work performed declares no binding ref. A publish error (W8). */
    | 'unbound-work'
    /** An element declares a binding ref that `workBindings` or `activities` does not resolve. */
    | 'unresolved-binding'
    /**
     * An event subprocess declares a listener binding ref that `workBindings` or `activities` does
     * not resolve.
     */
    | 'unresolved-listener-binding'
    /**
     * Two different nested processes reuse the same processId; the second, and everything it hosts,
     * is not shown.
     */
    | 'duplicate-process-id'
    /** A sequence flow names a source or target its scope does not contain; the flow is dropped. */
    | 'dangling-flow'
    /** A boundary event names a host its scope does not contain. */
    | 'unresolved-boundary-host'
    /** A compensation boundary event names a handler its scope does not contain. */
    | 'unresolved-compensation-handler'
    /** A lane names a pool the document's collaboration does not declare. */
    | 'unresolved-pool';

export type BpmnDiagnosticSeverity = 'info' | 'warning' | 'error';

/** One thing worth telling the user about the document, in document order. */
export interface BpmnDiagnostic {
    readonly code: BpmnDiagnosticCode;
    readonly severity: BpmnDiagnosticSeverity;
    readonly message: string;
    readonly elementId: string | null;
    readonly scopeId: string | null;
}

/** Where the diagram's coordinates came from, as a whole. */
export interface BpmnLayoutInfo {
    /**
     * `'document'` when at least one element was placed from BPMN DI, `'fallback'` when none was.
     * A document that places some but not all elements stays `'document'` and reports a
     * `missing-shape` diagnostic per element; check {@link BpmnViewElement.geometry}`.source` to
     * tell the two apart per element.
     */
    readonly source: BpmnGeometrySource;
    /** Why the layout fell back, or null when it did not. */
    readonly reason: string | null;
}

/** The whole diagram, canvas-neutral. */
export interface BpmnViewModel {
    /** The root scope's process id. */
    readonly processId: string;
    readonly name: string | null;
    readonly isExecutable: boolean;
    /** The root scope first, then nested scopes in depth-first document order. */
    readonly scopes: readonly BpmnViewScope[];
    /** Every element of every scope, in scope order then document order. */
    readonly elements: readonly BpmnViewElement[];
    /** Every drawable sequence flow. A flow with a dangling endpoint is reported, not listed. */
    readonly flows: readonly BpmnViewFlow[];
    /** Compensation boundary event to compensation handler associations. */
    readonly associations: readonly BpmnViewAssociation[];
    readonly lanes: readonly BpmnViewLane[];
    readonly pools: readonly BpmnViewPool[];
    readonly layout: BpmnLayoutInfo;
    readonly diagnostics: readonly BpmnDiagnostic[];
}
