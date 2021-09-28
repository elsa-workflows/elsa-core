import {ActivityModel} from "../../../../models";
import {Map} from "../../../../utils/utils";

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
  selectedActivities?: Map<ActivityModel> ;
}

export enum LayoutDirection {
  Horizontal,
  Vertical
}
