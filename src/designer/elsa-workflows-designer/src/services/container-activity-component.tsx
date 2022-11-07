import {AddActivityArgs, RenameActivityArgs, UpdateActivityArgs} from '../components/designer/canvas/canvas';
import {Activity} from "../models";

export interface ContainerActivityComponent {
  newRoot(): Promise<Activity>;
  updateLayout(): Promise<void>;
  addActivity(args: AddActivityArgs): Promise<Activity>;
  updateActivity(args: UpdateActivityArgs): Promise<void>;
  renameActivity(args: RenameActivityArgs): Promise<void>;
  export(): Promise<Activity>
  import(root: Activity): Promise<void>;
  zoomToFit(): Promise<void>;
  autoLayout(): Promise<void>;
  scrollToStart(): Promise<void>;
  reset(): Promise<void>;
  getCurrentLevel(): Promise<Activity>;
}
