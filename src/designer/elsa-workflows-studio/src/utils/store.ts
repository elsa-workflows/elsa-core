import { createStore } from "@stencil/store";

const { state, onChange } = createStore({
  activityDescriptors: [],
  monacoLibPath: ''
});

export default state;
