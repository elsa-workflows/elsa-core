import {Connection} from "./models";
import {Edge, Graph, Node} from "@antv/x6";
import {Activity} from "../../models";

export const FlowchartEvents = {
  ConnectionCreated: 'connection-created'
}

export interface ConnectionCreatedEventArgs {
  graph: Graph;
  connection: Connection;
  sourceNode: Node<Node.Properties>;
  targetNode: Node<Node.Properties>;
  sourceActivity: Activity;
  targetActivity: Activity;
  edge: Edge<Edge.Properties>;
}
