import {Activity, Port} from "./core";
import {WorkflowDefinition} from "../modules/workflow-definitions/models/entities";

export interface ActivitySelectedArgs {
  activity: Activity;
}

export interface ActivityDeletedArgs {
  activity: Activity;
}

export interface ContainerSelectedArgs {
  activity: Activity;
}

export interface GraphUpdatedArgs {
}

export interface WorkflowUpdatedArgs {
  workflowDefinition: WorkflowDefinition;
}

export interface EditChildActivityArgs {
  parentActivityId: string;
  port: Port;
}

export interface ChildActivitySelectedArgs {
  parentActivity: Activity;
  childActivity: Activity;
  port: Port;
}
