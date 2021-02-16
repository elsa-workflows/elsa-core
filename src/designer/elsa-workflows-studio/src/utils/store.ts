import { createStore } from "@stencil/store";

const { state, onChange } = createStore({
  activityDescriptors: []
});

export default state;
