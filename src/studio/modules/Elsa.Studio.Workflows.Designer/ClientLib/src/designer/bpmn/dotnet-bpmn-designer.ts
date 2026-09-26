/**
 * The bridge from the BPMN canvas back to .NET.
 *
 * The method names are the flowchart designer's -- `HandleActivitySelected`,
 * `HandleActivityDoubleClick`, `HandleCanvasSelected`, exactly as
 * `api/dotnet-flowchart-designer.ts` invokes them. That is deliberate and is the point of W9c's
 * "no BPMN-specific contract": W10 raises the same `ActivitySelected` and `ActivityDoubleClick`
 * events from a BPMN canvas as from a flowchart one, and anything downstream of those events --
 * the properties pane, the activity picker, the instance viewer -- keeps working without knowing
 * which canvas it is looking at.
 *
 * What differs is the payload. A flowchart node *is* an activity, so it sends one; a BPMN node is a
 * diagram element that may or may not have one, so it sends the element and names the activity
 * separately, which is null for a gateway, an event, or work nobody has bound yet.
 */
import type { DotNetComponentRef } from '../api/graph-bindings';

/** What a click on a BPMN element tells .NET. */
export interface BpmnElementSelection {
    /** The BPMN element id. Unique in the document, and the key the instance overlay uses. */
    readonly elementId: string;
    /** The raw BPMN element type, e.g. `serviceTask`, `boundaryEvent`. */
    readonly elementType: string;
    /** The element family: `event`, `gateway`, `task`, `callActivity`, `subProcess`, `unknown`. */
    readonly kind: string;
    readonly name: string | null;
    /** The Elsa activity bound to this element, or null when nothing is or should be. */
    readonly activityId: string | null;
    /** How the binding resolves, or null for an element that performs no work. */
    readonly bindingState: string | null;
    /**
     * `unboundTask` when the element's activity is authored on it (an `elsa:activityBinding` the user
     * binds by hand), `automatic` when the binder derives it from the document, or null for no work.
     */
    readonly bindingKind: string | null;
    /** The process id of the scope the element lives in. */
    readonly scopeId: string;
    /** The `Elsa.BpmnProcess` activity that runs that scope. */
    readonly scopeActivityId: string;
    /** For a boundary event, the element it is attached to. */
    readonly boundaryHostElementId: string | null;
    /** For a subprocess, the process id of the scope it contains. */
    readonly childScopeId: string | null;
}

export class DotNetBpmnDesigner {
    constructor(private readonly componentRef: DotNetComponentRef) {
    }

    /** Raises `ActivitySelected` on the .NET component. */
    async raiseElementSelected(selection: BpmnElementSelection): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleActivitySelected', selection);
    }

    /** Raises `ActivityDoubleClick` on the .NET component. */
    async raiseElementDoubleClick(selection: BpmnElementSelection): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleActivityDoubleClick', selection);
    }

    /** Raises `CanvasSelected` on the .NET component, i.e. nothing is selected any more. */
    async raiseCanvasSelected(): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleCanvasSelected');
    }
}
