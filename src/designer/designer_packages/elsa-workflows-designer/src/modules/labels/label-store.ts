import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  labels: [],
});

export default state;
