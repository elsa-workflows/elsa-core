import { createStore } from '@stencil/store';

const { state, onChange } = createStore({
  notifications: [],
  infoPanelBoolean: false,
});

export default state;
