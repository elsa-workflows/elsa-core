// GENERATED FILE -- do not edit by hand.
// Source: Bpmn.Model 0.2.0, payload format 1.0.0, schema/bpmn-payload.schema.json.
// Regenerate with: npm run generate:bpmn-types (from src/modules/Elsa.Studio.Workflows.Designer/ClientLib).
// See scripts/generate-bpmn-types.js.
/**
 * Version 1.0.0 of the payload format owned by Bpmn.Model. Generated from the model; do not edit by hand. See ADR 0005.
 */
export type BPMNPayloadFormat = (BpmnDefinitions | BpmnExecutionState)

export interface BpmnDefinitions {
id?: (string | null)
targetNamespace?: (string | null)
exporter?: (string | null)
exporterVersion?: (string | null)
collaboration?: (BpmnCollaboration | null)
processes: BpmnProcessDefinition[]
diagrams: BpmnDiagram[]
messages: BpmnMessageDeclaration[]
signals: BpmnSignalDeclaration[]
errors: BpmnErrorDeclaration[]
escalations: BpmnEscalationDeclaration[]
extensions: BpmnExtensions
}
export interface BpmnCollaboration {
id?: (string | null)
pools: BpmnPool[]
messageFlows: BpmnMessageFlow[]
extensions: BpmnExtensions
}
export interface BpmnPool {
poolId: string
name?: (string | null)
processRef?: (string | null)
isExecutable: boolean
extensions: BpmnExtensions
}
export interface BpmnExtensions {
documentation: BpmnDocumentation[]
extensionElements: BpmnExtensionElement[]
foreignAttributes: BpmnForeignAttribute[]
foreignChildren: BpmnForeignChild[]
}
export interface BpmnDocumentation {
text: string
textFormat?: (string | null)
}
export interface BpmnExtensionElement {
name: BpmnQName
value?: (string | null)
attributes: BpmnForeignAttribute[]
children: BpmnExtensionElement[]
}
export interface BpmnQName {
ns?: (string | null)
localName: string
}
export interface BpmnForeignAttribute {
name: BpmnQName
value: string
}
export interface BpmnForeignChild {
element: BpmnExtensionElement
index: number
}
export interface BpmnMessageFlow {
flowId: string
name?: (string | null)
sourceElementId?: (string | null)
sourcePoolId?: (string | null)
targetElementId?: (string | null)
targetPoolId?: (string | null)
messageName?: (string | null)
extensions: BpmnExtensions
}
export interface BpmnProcessDefinition {
processId: string
name?: (string | null)
isExecutable: boolean
isTransaction: boolean
elements: BpmnElement[]
sequenceFlows: BpmnSequenceFlow[]
lanes: BpmnLane[]
variables: BpmnVariableDeclaration[]
extensions: BpmnExtensions
}
export interface BpmnElement {
elementId: string
elementType: string
name?: (string | null)
bindingRef?: (string | null)
laneId?: (string | null)
defaultFlowId?: (string | null)
eventDefinitions: BpmnEventDefinition[]
properties: {
[k: string]: string
}
attachedToRef?: (string | null)
cancelActivity: boolean
loopCharacteristics?: (BpmnLoopCharacteristics | null)
isForCompensation: boolean
compensationHandlerElementId?: (string | null)
isTransaction: boolean
triggeredByEvent: boolean
listenerBindingRef?: (string | null)
extensions: BpmnExtensions
}
export interface BpmnEventDefinition {
type: string
properties: {
[k: string]: string
}
}
export interface BpmnLoopCharacteristics {
isSequential: boolean
cardinality?: (number | null)
collectionVariable?: (string | null)
itemVariable: string
}
export interface BpmnSequenceFlow {
flowId: string
sourceRef: string
targetRef: string
name?: (string | null)
conditionOutcome?: (string | null)
isDefault: boolean
extensions: BpmnExtensions
}
export interface BpmnLane {
laneId: string
poolId?: (string | null)
name?: (string | null)
extensions: BpmnExtensions
}
export interface BpmnVariableDeclaration {
name: string
typeHint?: (string | null)
defaultValue?: unknown
}
export interface BpmnDiagram {
id?: (string | null)
name?: (string | null)
plane: BpmnPlane
}
export interface BpmnPlane {
id?: (string | null)
bpmnElementRef?: (string | null)
shapes: BpmnShape[]
edges: BpmnEdge[]
}
export interface BpmnShape {
id?: (string | null)
bpmnElementRef: string
bounds: BpmnBounds
isHorizontal?: (boolean | null)
isExpanded?: (boolean | null)
isMarkerVisible?: (boolean | null)
label?: (BpmnLabel | null)
}
export interface BpmnBounds {
x: number
y: number
width: number
height: number
}
export interface BpmnLabel {
bounds?: (BpmnBounds | null)
}
export interface BpmnEdge {
id?: (string | null)
bpmnElementRef: string
label?: (BpmnLabel | null)
waypoints: BpmnPoint[]
}
export interface BpmnPoint {
x: number
y: number
}
export interface BpmnMessageDeclaration {
id: string
name?: (string | null)
}
export interface BpmnSignalDeclaration {
id: string
name?: (string | null)
}
export interface BpmnErrorDeclaration {
id: string
name?: (string | null)
errorCode?: (string | null)
}
export interface BpmnEscalationDeclaration {
id: string
name?: (string | null)
escalationCode?: (string | null)
}
export interface BpmnExecutionState {
tokens: BpmnToken[]
activeWork: BpmnActiveWork[]
diagnostics: BpmnDiagnosticEvent[]
sequence: number
races: BpmnEventRace[]
loops: BpmnLoopState[]
compensables: BpmnCompensable[]
compensationRuns: BpmnCompensationRun[]
terminated: boolean
cancelling: boolean
pendingFault?: (BpmnPendingFault | null)
}
export interface BpmnToken {
tokenId: string
atElementId: string
flowId?: (string | null)
parentTokenId?: (string | null)
status: BpmnTokenStatus
producingWorkHandle?: (string | null)
iterationKey?: (string | null)
kind?: (BpmnTokenKind | null)
}
export interface BpmnActiveWork {
nodeId: string
elementId: string
tokenId: string
schedulingCause: string
iterationId?: (string | null)
}
export interface BpmnDiagnosticEvent {
diagnosticId: string
kind: BpmnDiagnosticKind
message: string
elementId?: (string | null)
flowId?: (string | null)
tokenId?: (string | null)
details: {
[k: string]: string
}
}
export interface BpmnEventRace {
raceId: string
gatewayElementId: string
memberTokenIds: string[]
resolved: boolean
}
export interface BpmnLoopState {
loopId: string
tokenId: string
elementId: string
isSequential: boolean
totalCount: number
nextIndex: number
completedCount: number
items?: (unknown[] | null)
}
export interface BpmnCompensable {
compensableId: string
hostElementId: string
handlerElementId: string
status: BpmnCompensableStatus
}
export interface BpmnCompensationRun {
runId: string
throwTokenId: string
pendingCompensableIds: string[]
}
export interface BpmnPendingFault {
faultCode: string
message: string
}

/**
 * Serialized as an integer: the model declares no JsonStringEnumConverter.
 */
export enum BpmnTokenStatus {
Active = 0,
AwaitingChild = 1,
WaitingAtJoin = 2,
Consumed = 3,
Canceled = 4
}
/**
 * Serialized as an integer: the model declares no JsonStringEnumConverter.
 */
export enum BpmnTokenKind {
Listener = 0,
Activation = 1
}
/**
 * Serialized as an integer: the model declares no JsonStringEnumConverter.
 */
export enum BpmnDiagnosticKind {
TokenEmitted = 0,
Scheduled = 1,
Waiting = 2,
Joined = 3,
Consumed = 4,
Canceled = 5,
Terminated = 6,
BehaviorFailure = 7,
Completed = 8,
Faulted = 9,
CompensationRegistered = 10,
CompensationTriggered = 11,
Compensated = 12,
TransactionCancelled = 13,
EscalationRaised = 14,
EscalationCaught = 15,
EscalationUnhandled = 16,
EscalationLate = 17,
EventSubprocessActivated = 18,
EventSubprocessCompleted = 19,
CallActivityFailureRouted = 20,
ScopeListenerArmed = 21,
ScopeListenerFired = 22,
ScopeListenerRetired = 23
}
/**
 * Serialized as an integer: the model declares no JsonStringEnumConverter.
 */
export enum BpmnCompensableStatus {
Registered = 0,
Claimed = 1,
Compensated = 2
}

