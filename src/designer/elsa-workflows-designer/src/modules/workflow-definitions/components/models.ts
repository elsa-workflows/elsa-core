import {Activity, ActivityDescriptor, TabDefinition} from "../../../models";
import {ActivityInputContext} from "../../../services/activity-input-driver";

export interface RenderActivityPropsContext {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  title: string;
  inputs: Array<RenderActivityInputContext>;
  tabs: Array<TabDefinition>;
  selectedTabIndex: number;
}

export interface RenderActivityInputContext {
  inputContext: ActivityInputContext
  inputControl?: any;
}
