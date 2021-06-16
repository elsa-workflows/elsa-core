import {ActivityModel, WorkflowFault} from "../../../../models";

export enum WorkflowDesignerMode {
  Edit,
  Instance,
  Blueprint
}

export interface ActivityContextMenuState {
  shown: boolean;
  x: number;
  y: number;
  activity?: ActivityModel | null;
}

export enum LayoutDirection {
  Horizontal,
  Vertical
}