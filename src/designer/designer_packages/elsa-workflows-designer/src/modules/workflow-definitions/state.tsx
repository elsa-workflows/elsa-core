import {h} from "@stencil/core";
import {createProviderConsumer} from "@stencil/state-tunnel";
import {WorkflowDefinition} from "./models/entities";

export interface WorkflowDefinitionState {
  workflowDefinition: WorkflowDefinition;
}

export default createProviderConsumer<WorkflowDefinitionState>(
  {
    workflowDefinition: null,
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
