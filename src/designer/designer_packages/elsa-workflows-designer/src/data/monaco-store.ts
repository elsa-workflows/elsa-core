import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  monacoLibPath: ''
});

export default state;
