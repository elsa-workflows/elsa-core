import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";
import {Activity} from "../../models";
import {Hash} from "../../utils";
import {Flowchart} from './models';

export interface FlowchartState {
  flowchart: Flowchart;
  nodeMap: Hash<Activity>;
}

export default createProviderConsumer<FlowchartState>(
  {
    flowchart: null,
    nodeMap: {}
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
