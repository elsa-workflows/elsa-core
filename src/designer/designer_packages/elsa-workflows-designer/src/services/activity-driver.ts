import {Activity, ActivityDescriptor} from "../models";

export interface ActivityDisplayContext {
  activityDescriptor: ActivityDescriptor;
  displayType: ActivityDisplayType;
  activity?: Activity;
}

export interface ActivityDriver {
  display: (context: ActivityDisplayContext) => any;
}

export type ActivityDriverFactory = () => ActivityDriver;

export type ActivityDisplayType = string | 'designer' | 'picker';

