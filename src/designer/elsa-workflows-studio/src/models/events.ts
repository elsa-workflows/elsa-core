export const EventTypes = {
  ShowActivityPicker: 'show-activity-picker',
  ActivityPicked: 'activity-picked',
  ShowActivityEditor: 'show-activity-editor',
  UpdateActivity: 'update-activity',
  WorkflowModelChanged: 'workflow-model-changed'
}

export interface AddActivityEventArgs {
  sourceActivityId?: string
}

export interface ActivityPickedEventArgs {
  activityType: string
}
