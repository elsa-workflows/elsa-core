import { ActivityDefinitionProperty, WorkflowPersistenceBehavior, Connection } from './domain';
import { Map } from '../utils/utils';
export interface WorkflowModel {
  activities: Array<ActivityModel>;
  connections: Array<ConnectionModel>;
  persistenceBehavior?: WorkflowPersistenceBehavior;
  changeCounter?: number; //Only used in frontend to track changes and stop saving too often
}

export interface ActivityModel {
  activityId: string;
  type?: string;
  name?: string;
  displayName?: string;
  description?: string;
  outcomes?: Array<string>;
  properties?: Array<ActivityDefinitionProperty>;
  persistWorkflow?: boolean;
  loadWorkflowContext?: boolean;
  saveWorkflowContext?: boolean;
  propertyStorageProviders?: Map<string>;
  x?: number;
  y?: number;
  state?: any;
}

export interface ConnectionModel {
  sourceId: string;
  targetId: string;
  outcome?: string;
  targetPort?: string;
}
