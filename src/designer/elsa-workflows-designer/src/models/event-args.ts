import {Activity, Port} from "./core";

export interface ActivitySelectedArgs {
  activity: Activity;
  applyChanges: (activity: Activity) => void;
  deleteActivity: (activity: Activity) => void;
}

export interface ActivityDeletedArgs {
  activity: Activity;
}

export interface ContainerSelectedArgs {
  activity: Activity;
  applyChanges: (activity: Activity) => void;
}

export interface GraphUpdatedArgs {
  exportGraph: () => Activity;
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
