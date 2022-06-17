import {Activity} from "./core";

export interface ActivitySelectedArgs {
  activity: Activity;
  applyChanges: (activity: Activity) => void;
  deleteActivity: (activity: Activity) => void;
}

export interface ContainerSelectedArgs {
}

export interface GraphUpdatedArgs {
  exportGraph: () => Activity;
}
