import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";
import {Activity} from "../../models";
import {Hash} from "../../utils";
import {Flowchart} from './models';

export interface FlowchartState {
  nodeMap: Hash<Activity>;
}

export default createProviderConsumer<FlowchartState>(
  {
    nodeMap: {}
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
