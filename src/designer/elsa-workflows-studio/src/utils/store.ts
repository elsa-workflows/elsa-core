import { createStore } from "@stencil/store";

const { state, onChange } = createStore({
  activityDescriptors: [],
  workflowStorageDescriptors: [],
  monacoLibPath: ''
});

export default state;
