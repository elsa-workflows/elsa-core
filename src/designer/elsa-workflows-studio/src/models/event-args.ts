import {Edge, Graph, Node} from "@antv/x6";
import { Port } from "./core";
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
  port: Port;
}

export interface ChildActivitySelectedArgs {
  parentActivity: ActivityModel;
  childActivity: ActivityModel;
  port: Port;
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
