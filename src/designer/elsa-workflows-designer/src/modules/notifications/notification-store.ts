import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  notifications: [],
});

export default state;
