import {ActivityModel, WorkflowFault} from "../../../../models";

export enum WorkflowDesignerMode {
  Edit,
  Instance,
  Blueprint
}

interface AggregatedActivityEvent {
  eventName: string;
  count: number;
}

export interface ActivityStats {
  fault?: WorkflowFault;
  averageExecutionTime: number;
  fastestExecutionTime: number;
  slowestExecutionTime: number;
  lastExecutedAt: Date;
  events: Array<AggregatedActivityEvent>;
} 

export interface ActivityContextMenuState {
  shown: boolean;
  x: number;
  y: number;
  activity?: ActivityModel | null;
}