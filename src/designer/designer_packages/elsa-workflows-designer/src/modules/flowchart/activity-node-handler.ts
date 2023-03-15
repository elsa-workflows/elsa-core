import {Node} from "@antv/x6";
import {Activity, ActivityDescriptor} from "../../models";

export interface ActivityNodeHandler {
  createDesignerNode: (context: UINodeContext) => Node.Metadata;
  createPorts: (context: UIPortContext) => Array<any>;
}

export interface UINodeContext extends UIPortContext{
  x: number;
  y: number;
}

export interface UIPortContext {
  activityDescriptor: ActivityDescriptor;
  activity: Activity;
}
