import {Activity, ActivityDescriptor, Container} from '../../models';

export interface Flowchart extends Container {
  start: string;
  connections: Array<Connection>;
}

export interface Connection {
  source: string;
  target: string;
  sourcePort: string;
  targetPort: string;
}

export interface FlowchartNavigationItem {
  activityId: string;
  portName?: string;
  index: number;
}

export interface FlowchartPathItem {
  activityId: string;
  portName: string;
}

export interface FlowchartModel {
  activities: Array<Activity>;
  connections: Array<Connection>;
  start: string;
}

export interface AddActivityArgs {
  descriptor: ActivityDescriptor;
  id?: string;
  x?: number;
  y?: number;
}

export interface UpdateActivityArgs {
  id: string;
  originalId: string;
  activity: Activity;
}

export interface RenameActivityArgs {
  originalId: string;
  newId: string;
  activity: Activity;
}

export type LayoutDirection = 'LR' | 'TB' | 'RL' | 'BT';
