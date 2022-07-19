import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  items: [],
});

export default state;
