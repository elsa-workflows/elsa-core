export { DesignerProvider, useDesigner, useDesignerState, type ActivityLink } from "./context.js";
export { createDesignerStore, revealActivity, type DesignerStore, type DesignerState } from "./store.js";
export { ActivityRegistry } from "./model/registry.js";
export { computePorts } from "./model/ports.js";
export { activityDisplayText, activityDescription } from "./model/display.js";
export { activityIcon, activityAccent, activityAccentColor } from "./model/icons.js";
export { emptyFlowchart, emptySequence, emptyStateMachine, emptyContainer, indexActivities, resolveContainer, type PathSegment } from "./model/flowchart-ops.js";
export { containerKind, isDesignableContainer, listSlots, childActivities, getSlot, CONTAINER_TYPES, type SlotPath } from "./model/containers.js";
export { FLOW_TYPES, type FlowType } from "./model/flow-types.js";
export { selectionId, stateMachineDesignerMetadata } from "./store.js";
export { useActivityRegistry } from "./hooks.js";
export { validateWorkflow, validateFlowchart, issuesByActivity, type DesignerIssue } from "./model/validation.js";
export { indexDataFlow, dataRefKey, expressionCompletions, type DataFlowIndex, type DataUsage, type DataUse } from "./model/data-flow.js";
export {
  ACTIVITY_CONTEXT_HANDLE,
  CONTEXT_BOX_ID,
  DECORATION_PREFIX,
  isDecorationId,
  type CanvasDecorations,
  type ContextBoxNode,
  type ContextWorkflowNode,
  type DecorationEdge,
  type DecorationNode,
  type DecorationNodeKind,
} from "./decorations.js";
export { DesignerSurface, ActivityToolbox, StatePanel, TransitionPanel } from "../components.js";
