export interface WorkflowModel {
  activities: Array<ActivityModel>
  connections: Array<ConnectionModel>
}

export interface ActivityModel {
  activityId: string
  type: string
  name: string
  displayName: string
  description: string
  outcomes: Array<string>
}

export interface ConnectionModel {
  sourceId: string,
  targetId: string,
  outcome: string
}

export interface ActivityDescriptor {
  type: string,
  displayName: string,
  description?: string,
  category: string,
  traits: ActivityTraits,
  outcomes: Array<string>,
  browsable: boolean,
  properties: Array<ActivityPropertyDescriptor>
}

export interface ActivityPropertyDescriptor {
  name: string,
  type: string,
  label?: string,
  hint?: string,
  options?: any
}

export enum ActivityTraits {
  Action = 1,
  Trigger = 2,
  Job = 4
}
