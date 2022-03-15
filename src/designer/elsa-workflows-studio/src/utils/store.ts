import { createStore } from "@stencil/store";

const { state, onChange } = createStore({
  activityDescriptors: [],
  workflowStorageDescriptors: [],
  secretsDescriptors: [],
  monacoLibPath: ''
});

export default state;
