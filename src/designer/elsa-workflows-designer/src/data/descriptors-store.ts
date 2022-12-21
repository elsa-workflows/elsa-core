import { createStore } from '@stencil/store';
import {ActivityDescriptor, WorkflowActivationStrategyDescriptor} from "../models";
import {VariableDescriptor} from "../services/api-client/variable-descriptors-api";

export interface DescriptorsStore {
  activityDescriptors: Array<ActivityDescriptor>;
  storageDrivers: Array<any>;
  variableDescriptors: Array<VariableDescriptor>;
  workflowActivationStrategyDescriptors: Array<WorkflowActivationStrategyDescriptor>;
}

const { state, onChange } = createStore<DescriptorsStore>({
  activityDescriptors: [],
  storageDrivers: [],
  variableDescriptors: [],
  workflowActivationStrategyDescriptors: []
} as DescriptorsStore);

export default state;
