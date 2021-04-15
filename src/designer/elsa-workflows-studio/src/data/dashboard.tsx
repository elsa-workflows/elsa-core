import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";

export interface DashboardState {
  serverUrl: string;
}

export default createProviderConsumer<DashboardState>(
  {
    serverUrl: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
