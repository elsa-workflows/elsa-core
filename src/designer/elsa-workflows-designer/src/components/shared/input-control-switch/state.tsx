import {h} from "@stencil/core";
import {createProviderConsumer} from "@stencil/state-tunnel";

export interface InputControlSwitchContextState {
  workflowDefinitionId: string;
  activityType: string;
  propertyName: string;
}

export default createProviderConsumer<InputControlSwitchContextState>(
  {
    workflowDefinitionId: null,
    activityType: null,
    propertyName: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
