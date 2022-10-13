import {ActivityModel} from "../../../../models";
import {Map} from "../../../../utils/utils";

export enum WorkflowDesignerMode {
  Edit,
  Instance,
  Blueprint,
  Test
}

export interface ActivityContextMenuState {
  shown: boolean;
  x: number;
  y: number;
  activity?: ActivityModel | null;
  selectedActivities?: Map<ActivityModel> ;
}

export enum LayoutDirection {
  LeftRight ='leftright',
  TopBottom = 'topbottom',
  RightLeft = 'rightleft',
  BottomTop = 'bottomtop'
}

export interface LayoutState {
  dragging: boolean,
  nodeDragging: boolean,
  ratio: number,
  scale: number,
  left: number,
  top: number,
  initX: number,
  initY: number
}
