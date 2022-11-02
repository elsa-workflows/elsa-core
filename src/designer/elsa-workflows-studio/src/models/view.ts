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
  x?: number;
  y?: number;
  state?: any;
  metadata?: any;
  canStartWorkflow?: boolean;
  applicationProperties?: any;
}

export interface ConnectionModel {
  sourceId: string;
  targetId: string;
  outcome?: string;
  targetPort?: string;
}

export interface Flowchart extends Container {
  id: string,
  start: string;
  connections: Array<ConnectionModel>;
  version?: number;
}

export interface Container extends ActivityModel {
  activities: Array<ActivityModel>;
  variables: Array<Variable>;
}

export interface Variable {
  name: string;
  type: string;
  value?: any;
  storageDriverId?: string;
}
