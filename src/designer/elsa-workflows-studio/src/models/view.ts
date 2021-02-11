import {ActivityDefinitionProperties} from "./domain";

export interface WorkflowModel {
  activities: Array<ActivityModel>
  connections: Array<ConnectionModel>
}

export interface ActivityModel {
  activityId: string
  type: string
  name?: string
  displayName?: string
  description?: string
  outcomes: Array<string>
  properties: ActivityDefinitionProperties;
}

export interface ConnectionModel {
  sourceId: string,
  targetId: string,
  outcome: string
}
