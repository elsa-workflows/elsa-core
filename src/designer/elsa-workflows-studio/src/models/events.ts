export const EventTypes = {
  ShowActivityPicker: 'show-activity-picker',
  ShowWorkflowSettings: 'show-workflow-settings',
  ActivityPicked: 'activity-picked',
  ShowActivityEditor: 'show-activity-editor',
  UpdateActivity: 'update-activity',
  UpdateWorkflowSettings: 'update-workflow-settings',
  WorkflowModelChanged: 'workflow-model-changed'
}

export interface AddActivityEventArgs {
  sourceActivityId?: string
}

export interface ActivityPickedEventArgs {
  activityType: string
}
