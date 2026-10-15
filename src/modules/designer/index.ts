export { DesignerProvider, useDesigner, useDesignerState, type ActivityLink } from "./context";
export { createDesignerStore, revealActivity, type DesignerStore, type DesignerState } from "./store";
export { DesignerCanvas, IconButton } from "./components/designer-canvas";
export { DesignerSurface } from "./components/designer-surface";
export { SequenceDesigner } from "./components/sequence-designer";
export { StateMachineCanvas } from "./components/state-machine-canvas";
export { StatePanel } from "./components/state-panel";
export { TransitionPanel } from "./components/transition-panel";
export { SlotChip } from "./components/slot-chip";
export { ActivityCard } from "./components/activity-card";
export { ActivityToolbox } from "./components/activity-toolbox";
export { ActivityRegistry } from "./model/registry";
export { computePorts } from "./model/ports";
export { activityDisplayText, activityDescription } from "./model/display";
export { activityIcon, activityAccent, activityAccentColor } from "./model/icons";
export { ActivityIcon } from "./components/activity-icon";
export { emptyFlowchart, emptySequence, emptyStateMachine, emptyContainer, indexActivities, resolveContainer, type PathSegment } from "./model/flowchart-ops";
export { containerKind, isDesignableContainer, listSlots, childActivities, getSlot, CONTAINER_TYPES, type SlotPath } from "./model/containers";
export { FLOW_TYPES, type FlowType } from "./model/flow-types";
export { selectionId, stateMachineDesignerMetadata } from "./store";
export { useActivityRegistry } from "./hooks";
export { validateWorkflow, validateFlowchart, issuesByActivity, type DesignerIssue } from "./model/validation";
export { indexDataFlow, dataRefKey, expressionCompletions, type DataFlowIndex, type DataUsage, type DataUse } from "./model/data-flow";
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
} from "./components/decorations";
