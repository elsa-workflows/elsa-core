import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";

export interface WorkflowEditorState {
  workflowDefinitionId: string;
  serverUrl: string;
  serverFeatures: Array<string>;
}

export default createProviderConsumer<WorkflowEditorState>(
  {
    workflowDefinitionId: null,
    serverUrl: null,
    serverFeatures: []
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
