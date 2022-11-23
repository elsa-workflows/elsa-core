import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";
import {WorkflowDefinition} from "./models/entities";

export interface WorkflowDefinitionEditorState {
  workflowDefinition: WorkflowDefinition;
}

export default createProviderConsumer<WorkflowDefinitionEditorState>(
  {
    workflowDefinition: null,
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
