import {ActivityModel} from "./view";
import {ActivityDescriptor} from "./domain";

export const EventTypes = {
  ShowActivityPicker: 'show-activity-picker',
  ShowWorkflowSettings: 'show-workflow-settings',
  ActivityPicked: 'activity-picked',
  ShowActivityEditor: 'show-activity-editor',
  UpdateActivity: 'update-activity',
  UpdateWorkflowSettings: 'update-workflow-settings',
  WorkflowModelChanged: 'workflow-model-changed',
  ActivityDesignDisplaying: 'activity-design-displaying',
  ActivityDescriptorDisplaying: 'activity-descriptor-displaying',
  WorkflowPublished: 'workflow-published',
  WorkflowRetracted: 'workflow-retracted',
  WorkflowImported: 'workflow-imported',
};

export interface AddActivityEventArgs {
  sourceActivityId?: string;
}

export interface ActivityPickedEventArgs {
  activityType: string;
}

export interface ActivityDesignDisplayContext {
  activityModel: ActivityModel;
  activityIcon: any;
  displayName?: string;
  bodyDisplay: string;
  outcomes: Array<string>;
}

export interface ActivityDescriptorDisplayContext {
  activityDescriptor: ActivityDescriptor;
  activityIcon: any;
}
