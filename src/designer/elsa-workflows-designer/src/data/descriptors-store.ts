import { createStore } from '@stencil/store';
import {ActivityDescriptor, WorkflowInstantiationStrategyDescriptor} from "../models";
import {VariableDescriptor} from "../services/api-client/variable-descriptors-api";

export interface DescriptorsStore {
  activityDescriptors: Array<ActivityDescriptor>;
  storageDrivers: Array<any>;
  variableDescriptors: Array<VariableDescriptor>;
  workflowInstantiationStrategyDescriptors: Array<WorkflowInstantiationStrategyDescriptor>;
}

const { state, onChange } = createStore<DescriptorsStore>({
  activityDescriptors: [],
  storageDrivers: [],
  variableDescriptors: [],
  workflowInstantiationStrategyDescriptors: []
} as DescriptorsStore);

export default state;
