import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";

export interface DashboardState {
  serverUrl: string;
  basePath: string;
  culture: string;
  monacoLibPath: string;
  serverFeatures: Array<string>;
  serverVersion: string;
}

export default createProviderConsumer<DashboardState>(
  {
    serverUrl: null,
    basePath: null,
    culture: null,
    monacoLibPath: null,
    serverFeatures: [],
    serverVersion: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
