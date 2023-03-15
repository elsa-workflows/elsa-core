import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  serverAddress: '',
});

export default state;
