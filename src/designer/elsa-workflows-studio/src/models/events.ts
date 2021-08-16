import {ActivityModel} from "./view";
import {ActivityDescriptor} from "./domain";

export const EventTypes = {
  ShowActivityPicker: 'show-activity-picker',
  ShowWorkflowSettings: 'show-workflow-settings',
  ActivityPicked: 'activity-picked',
  ShowActivityEditor: 'show-activity-editor',
  ActivityEditorDisplaying: 'activity-editor-displaying',
  UpdateActivity: 'update-activity',
  UpdateWorkflowSettings: 'update-workflow-settings',
  WorkflowModelChanged: 'workflow-model-changed',
  ActivityDesignDisplaying: 'activity-design-displaying',
  ActivityDescriptorDisplaying: 'activity-descriptor-displaying',
  WorkflowPublished: 'workflow-published',
  WorkflowRetracted: 'workflow-retracted',
  WorkflowImported: 'workflow-imported',
  WorkflowUpdated: 'workflow-updated',
  HttpClientConfigCreated: 'http-client-config-created',
  HttpClientCreated: 'http-client-created',
  WorkflowInstanceBulkActionsLoading: 'workflow-instance-bulk-actions-loading',
  ShowConfirmDialog: 'show-confirm-dialog',
  HideConfirmDialog: 'hide-confirm-dialog',
  ShowToastNotification: 'show-toast-notification',
  HideToastNotification: 'hide-toast-notification',
  WebhooksEnabled: 'webhooks-enabled',
  WorkflowSettingsEnabled: 'workflow-settings-enabled',
  WorkflowSettingsUpdated: 'workflow-settings-updated',
};

export interface AddActivityEventArgs {
  sourceActivityId?: string;
}

export interface ActivityPickedEventArgs {
  activityType: string;
}

export interface ActivityDesignDisplayContext {
  activityModel: ActivityModel;
  activityDescriptor: ActivityDescriptor;
  activityIcon: any;
  displayName?: string;
  bodyDisplay: string;
  outcomes: Array<string>;
}

export interface ActivityDescriptorDisplayContext {
  activityDescriptor: ActivityDescriptor;
  activityIcon: any;
}

export interface WebhooksEnabledContext {
  isEnabled: boolean;
}

export interface WorkflowSettingsEnabledContext {
  isEnabled: boolean;
}

export interface WorkflowSettingsUpdatedContext {
  workflowBlueprintId?: string;
  key?: string;
  value?: string;
}