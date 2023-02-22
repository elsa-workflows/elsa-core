import {Activity, ActivityDescriptor, TabDefinition} from "../../../models";
import {ActivityInputContext} from "../../../services/activity-input-driver";

export interface RenderActivityPropsContext {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  title: string;
  inputs: Array<RenderActivityInputContext>;
  tabs: Array<TabDefinition>;
  selectedTabIndex: number;
  notifyActivityChanged: () => void;
}

export interface RenderActivityInputContext {
  inputContext: ActivityInputContext
  inputControl?: any;
}

export interface WorkflowDefinitionActivity extends Activity {
  alwaysUsePublishedVersion: boolean;
  useVersion: number;
}
