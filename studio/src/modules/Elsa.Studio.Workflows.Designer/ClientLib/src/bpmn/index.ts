/**
 * The canvas-neutral BPMN view model. See ./README.md.
 *
 * This is the only entry point either designer adapter -- or .NET JS interop -- should import from.
 */
export { buildBpmnViewModel } from './view-model';
export {
    BOUNDARY_EVENT_ELEMENT_TYPE,
    CALL_ACTIVITY_ELEMENT_TYPE,
    EVENT_ELEMENT_TYPES,
    GATEWAY_ELEMENT_TYPES,
    SUB_PROCESS_ELEMENT_TYPE,
    TASK_ELEMENT_TYPES,
    MESSAGE_NAME_PROPERTY_KEY,
    classifyElementType,
    defaultSizeFor,
    isUnboundTask,
    requiresWorkBinding,
} from './element-kinds';
export type {
    BpmnActivity,
    BpmnActivityDescriptor,
    BpmnActivityStats,
    BpmnBinding,
    BpmnBindingKind,
    BpmnBindingState,
    BpmnBoundaryAttachment,
    BpmnDiagnostic,
    BpmnDiagnosticCode,
    BpmnDiagnosticSeverity,
    BpmnElementKind,
    BpmnElementStats,
    BpmnGeometrySource,
    BpmnLayoutInfo,
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
export type {
    BpmnBounds,
    BpmnElement,
    BpmnEventDefinition,
    BpmnExtensions,
    BpmnLoopCharacteristics,
    BpmnPoint,
    BpmnProcessDefinition,
    BpmnSequenceFlow,
} from './types.generated';
