import {Activity} from "../models";
import {AddActivityArgs, RenameActivityArgs, UpdateActivityArgs} from "../modules/flowchart/models";

export interface ContainerActivityComponent {
  newRoot(): Promise<Activity>;
  updateLayout(): Promise<void>;
  addActivity(args: AddActivityArgs): Promise<Activity>;
  updateActivity(args: UpdateActivityArgs): Promise<void>;
  renameActivity(args: RenameActivityArgs): Promise<void>;
  export(): Promise<Activity>
  import(root: Activity): Promise<void>;
  zoomToFit(): Promise<void>;
  autoLayout(direction: "TB" | "BT" | "LR" | "RL"): Promise<void>;
  scrollToStart(): Promise<void>;
  reset(): Promise<void>;
  //getCurrentLevel(): Promise<Activity>;
}
