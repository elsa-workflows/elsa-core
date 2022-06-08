import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";
import {ActivityDescriptor, WorkflowDefinition} from "../../models";

export interface WorkflowDesignerState {
  workflowDefinition: WorkflowDefinition;
}

export default createProviderConsumer<WorkflowDesignerState>(
  {
    workflowDefinition: null,
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
