import {Activity, ActivityDescriptor, InputDescriptor} from "../models";

export interface ActivityInputContext {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  inputDescriptor: InputDescriptor;
  notifyInputChanged: () => void;
  inputChanged: (value: any, syntax: string) => void;
}

export interface ActivityInputDriver {
  supportsInput(context: ActivityInputContext): boolean;

  get priority(): number;

  renderInput(context: ActivityInputContext): any;
}
