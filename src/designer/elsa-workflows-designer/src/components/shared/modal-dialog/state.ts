import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  instances: []
});

export default state;
