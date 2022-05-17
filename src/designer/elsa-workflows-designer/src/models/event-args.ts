import {Activity} from "./core";
import {ActivityDescriptor, InputDescriptor} from "./api";

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

export interface ActivityPropertyChangedEventArgs {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  propertyName?: string;
  inputDescriptor?: InputDescriptor;
  workflowEditor: HTMLElsaWorkflowEditorElement;
}
