import {Edge, Graph, Node} from "@antv/x6";
import { ActivityModel, ConnectionModel } from "./view";

export interface ActivitySelectedArgs {
  activity: ActivityModel;
}

export interface ActivityDeletedArgs {
  activity: ActivityModel;
}

export interface ContainerSelectedArgs {
  activity: ActivityModel;
}

export interface GraphUpdatedArgs {
}

export interface EditChildActivityArgs {
  parentActivityId: string;
}

export interface ChildActivitySelectedArgs {
  parentActivity: ActivityModel;
  childActivity: ActivityModel;
}

export const FlowchartEvents = {
  ConnectionCreated: 'connection-created'
}

export interface ConnectionCreatedEventArgs {
  graph: Graph;
  connection: ConnectionModel;
  sourceNode: Node<Node.Properties>;
  targetNode: Node<Node.Properties>;
  sourceActivity: ActivityModel;
  targetActivity: ActivityModel;
  edge: Edge<Edge.Properties>;
}
