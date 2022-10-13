import { ActivityDefinitionProperty, WorkflowPersistenceBehavior, Connection } from './domain';
import { Map } from '../utils/utils';
export interface WorkflowModel {
  activities: Array<ActivityModel>;
  connections: Array<ConnectionModel>;
  persistenceBehavior?: WorkflowPersistenceBehavior;
  start?: string;
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
  left?: number;
  top?: number;
  state?: any;
}

export interface ConnectionModel {
  sourceId: string;
  targetId: string;
  outcome?: string;
}
