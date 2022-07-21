import {Node} from "@antv/x6";
import {Activity, ActivityDescriptor} from "../../models";

export interface ActivityNodeHandler {
  createDesignerNode: (context: CreateUINodeContext) => Node.Metadata;
}

export interface CreateUINodeContext {
  activityDescriptor: ActivityDescriptor;
  activity: Activity;
  x: number;
  y: number;
}
