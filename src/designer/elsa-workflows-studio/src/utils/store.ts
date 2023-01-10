import { createStore } from "@stencil/store";

const { state, onChange } = createStore({
  activityDescriptors: [],
  workflowStorageDescriptors: [],
  monacoLibPath: '',
  useX6Graphs: false
});

export default state;
