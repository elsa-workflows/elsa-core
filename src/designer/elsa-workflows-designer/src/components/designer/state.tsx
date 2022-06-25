import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";
import {Activity, WorkflowDefinition} from "../../models";
import {Hash} from "../../utils";

export interface WorkflowDesignerState {
  workflowDefinition: WorkflowDefinition;
  nodeMap: Hash<Activity>;
}

export default createProviderConsumer<WorkflowDesignerState>(
  {
    workflowDefinition: null,
    nodeMap: {}
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
