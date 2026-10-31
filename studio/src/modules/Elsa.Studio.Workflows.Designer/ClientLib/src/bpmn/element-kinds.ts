/**
 * Classification of the `elementType` strings the payload carries.
 *
 * The payload schema types `BpmnElement.elementType` as a plain string, so there is no generated
 * union to switch on; these tables mirror `Bpmn.Model.BpmnElementTypes`, which is where the values
 * come from. An `elementType` that appears in none of them classifies as `'unknown'` and is still
 * rendered -- a library that adds an element kind should show up on the canvas as a placeholder,
 * not disappear from it.
 */
import type { BpmnElementKind } from './model';

/** The eight task-family element types. `callActivity` behaves as one but is its own kind here. */
export const TASK_ELEMENT_TYPES: readonly string[] = [
    'task',
    'userTask',
    'serviceTask',
    'scriptTask',
    'manualTask',
    'businessRuleTask',
    'sendTask',
    'receiveTask',
];

export const GATEWAY_ELEMENT_TYPES: readonly string[] = [
    'exclusiveGateway',
    'parallelGateway',
    'inclusiveGateway',
    'eventBasedGateway',
];

export const EVENT_ELEMENT_TYPES: readonly string[] = [
    'startEvent',
    'endEvent',
    'intermediateCatchEvent',
    'intermediateThrowEvent',
    'boundaryEvent',
];

export const BOUNDARY_EVENT_ELEMENT_TYPE = 'boundaryEvent';
export const SUB_PROCESS_ELEMENT_TYPE = 'subProcess';
export const CALL_ACTIVITY_ELEMENT_TYPE = 'callActivity';

const KIND_BY_ELEMENT_TYPE: ReadonlyMap<string, BpmnElementKind> = new Map<string, BpmnElementKind>([
    ...TASK_ELEMENT_TYPES.map(type => [type, 'task'] as const),
    ...GATEWAY_ELEMENT_TYPES.map(type => [type, 'gateway'] as const),
    ...EVENT_ELEMENT_TYPES.map(type => [type, 'event'] as const),
    [CALL_ACTIVITY_ELEMENT_TYPE, 'callActivity'],
    [SUB_PROCESS_ELEMENT_TYPE, 'subProcess'],
]);

export function classifyElementType(elementType: string): BpmnElementKind {
    return KIND_BY_ELEMENT_TYPE.get(elementType) ?? 'unknown';
}

/**
 * Whether an element of this type performs work the host has to supply, and is therefore
 * *unbound* -- a visible state, and a publish error under W8 -- when it declares no binding ref.
 *
 * The eight tasks, `callActivity` and `subProcess` are exactly the kinds `BpmnWorkBinder` refuses
 * to bind without one. An event is deliberately not on this list: a plain start or end event
 * legitimately binds nothing, so absence of a binding there is not a defect to report.
 */
export function requiresWorkBinding(elementType: string): boolean {
    return TASK_ELEMENT_TYPES.includes(elementType)
        || elementType === CALL_ACTIVITY_ELEMENT_TYPE
        || elementType === SUB_PROCESS_ELEMENT_TYPE;
}

/**
 * The element `properties` key the reader records a send or receive task's resolved message name under
 * (`Bpmn.Interchange.BpmnXmlReader.MessageNamePropertyKey`). Only that resolution ever sets it, which is
 * what tells a message send or receive task -- bound automatically -- apart from one whose work the host
 * has to supply.
 */
export const MESSAGE_NAME_PROPERTY_KEY = 'bpmn.messageName';

/**
 * Whether an element's work is *authored*: `Bpmn.Interchange`'s `BpmnWorkBinding.UnboundTask`, a task
 * the document describes without saying how to perform it, whose Elsa activity comes from an
 * `elsa:activityBinding` declaration on the element itself.
 *
 * Every task kind is one, except a send or receive task that resolved a message: that is a message
 * publish or wait the binder binds on its own. The same rule elsa-core's `ValidateBpmnProcessBindings`
 * applies when it decides which elements the publish gate checks.
 */
export function isUnboundTask(elementType: string, properties: Readonly<Record<string, string>> | null | undefined): boolean {
    return TASK_ELEMENT_TYPES.includes(elementType) && properties?.[MESSAGE_NAME_PROPERTY_KEY] == null;
}

/**
 * The size a fallback layout gives an element of this kind, in BPMN DI units.
 *
 * These are the sizes the BPMN 2.0 DI examples and every mainstream modeller use, so a fallback
 * diagram reads at the same scale as one that carries real coordinates.
 */
export function defaultSizeFor(elementType: string): { width: number; height: number } {
    switch (classifyElementType(elementType)) {
        case 'event':
            return { width: 36, height: 36 };
        case 'gateway':
            return { width: 50, height: 50 };
        default:
            return { width: 100, height: 80 };
    }
}
