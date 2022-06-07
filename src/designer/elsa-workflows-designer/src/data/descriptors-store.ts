import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  activityDescriptors: [],
  storageDrivers: []
});

export default state;
