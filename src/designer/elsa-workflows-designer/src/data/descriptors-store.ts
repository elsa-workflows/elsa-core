import { createStore } from '@stencil/store';
import {ActivityDescriptor} from "../models";

export interface DescriptorsStore {
  activityDescriptors: Array<ActivityDescriptor>;
  storageDrivers: Array<any>;
}

const { state, onChange } = createStore({
  activityDescriptors: [],
  storageDrivers: []
} as DescriptorsStore);

export default state;
