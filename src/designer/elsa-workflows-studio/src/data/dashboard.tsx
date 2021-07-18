import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";

export interface DashboardState {
  serverUrl: string;
  culture: string;
  monacoLibPath: string;
}

export default createProviderConsumer<DashboardState>(
  {
    serverUrl: null,
    culture: null,
    monacoLibPath: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
