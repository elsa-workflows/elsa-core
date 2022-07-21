import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  items: [],
  mainItem: null
});

export default state;
