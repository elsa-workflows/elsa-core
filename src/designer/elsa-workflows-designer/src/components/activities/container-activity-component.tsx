import {AddActivityArgs} from '../designer/canvas/canvas';
import {Activity} from '../../models';

export interface ContainerActivityComponent {
  updateLayout(): Promise<void>;
  addActivity(args: AddActivityArgs): Promise<void>;
  exportRoot(): Promise<Activity>
  importRoot(root: Activity): Promise<void>;
}
