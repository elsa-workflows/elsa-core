import {Activity, Container} from '../../../models';

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

export interface FlowchartModel {
  activities: Array<Activity>;
  connections: Array<Connection>;
  start: string;
}
