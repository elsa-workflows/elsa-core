export const EventTypes = {
  ShowActivityPicker: 'show-activity-picker',
  ActivityPicked: 'activity-picked',
  ShowActivityEditor: 'show-activity-editor'
}

export interface AddActivityEventArgs {
  sourceActivityId?: string
}

export interface ActivityPickedEventArgs {
  activityType: string
}
