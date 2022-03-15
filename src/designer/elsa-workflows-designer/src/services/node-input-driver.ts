import {Activity, ActivityDescriptor, InputDescriptor} from "../models";

export interface ActivityInputContext {
  node: Activity;
  nodeDescriptor: ActivityDescriptor;
  inputDescriptor: InputDescriptor;
  notifyInputChanged: () => void;
  inputChanged: (value: any, syntax: string) => void;
}

export interface NodeInputDriver {
  supportsInput(context: ActivityInputContext): boolean;

  get priority(): number;

  renderInput(context: ActivityInputContext): any;
}
