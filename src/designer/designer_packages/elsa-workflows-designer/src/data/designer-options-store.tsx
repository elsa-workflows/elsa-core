import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  enableFlexiblePorts: false,
});

export default state;
