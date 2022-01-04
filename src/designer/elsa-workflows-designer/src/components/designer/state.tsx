import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";
import {ActivityDescriptor, TriggerDescriptor, Workflow} from "../../models";

export interface WorkflowDesignerState {
  workflow: Workflow;
  activityDescriptors: Array<ActivityDescriptor>;
  triggerDescriptors: Array<TriggerDescriptor>;
}

export default createProviderConsumer<WorkflowDesignerState>(
  {
    workflow: null,
    activityDescriptors: [],
    triggerDescriptors: []
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
