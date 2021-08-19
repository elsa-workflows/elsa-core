import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";

export interface DashboardState {
  serverUrl: string;
  basePath: string;
  culture: string;
  featuresString: string;
  monacoLibPath: string;
}

export default createProviderConsumer<DashboardState>(
  {
    serverUrl: null,
    basePath: null,
    culture: null,
    featuresString: null,
    monacoLibPath: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
