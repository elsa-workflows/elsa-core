import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  serverAddress: '',
  monacoLibPath: ''
});

export default state;
