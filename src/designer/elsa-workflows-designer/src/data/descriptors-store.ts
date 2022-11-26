import { createStore } from '@stencil/store';
import {ActivityDescriptor} from "../models";
import {VariableDescriptor} from "../services/api-client/variable-descriptors-api";

export interface DescriptorsStore {
  activityDescriptors: Array<ActivityDescriptor>;
  storageDrivers: Array<any>;
  variableDescriptors: Array<VariableDescriptor>;
}

const { state, onChange } = createStore({
  activityDescriptors: [],
  storageDrivers: [],
  variableDescriptors: []
} as DescriptorsStore);

export default state;
