import {AddActivityArgs} from '../designer/canvas/canvas';
import {Activity} from '../../models';

export interface ContainerActivityComponent {
  updateLayout(): Promise<void>;
  addActivity(args: AddActivityArgs): Promise<void>;
  export(): Promise<Activity>
  import(root: Activity): Promise<void>;
  zoomToFit(): Promise<void>;
  clear(): Promise<void>;
}
