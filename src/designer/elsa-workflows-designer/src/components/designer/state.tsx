import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";
import {ActivityDescriptor, WorkflowDefinition} from "../../models";

export interface WorkflowDesignerState {
  workflow: WorkflowDefinition;
  activityDescriptors: Array<ActivityDescriptor>;
}

export default createProviderConsumer<WorkflowDesignerState>(
  {
    workflow: null,
    activityDescriptors: []
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
