import {h} from "@stencil/core";
import {createProviderConsumer} from "@stencil/state-tunnel";

export interface InputControlSwitchContextState {
  containerType: string;
  containerId: string;
  activityType: string;
  propertyName: string;
}

export default createProviderConsumer<InputControlSwitchContextState>(
  {
    containerType: null,
    containerId: null,
    activityType: null,
    propertyName: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
