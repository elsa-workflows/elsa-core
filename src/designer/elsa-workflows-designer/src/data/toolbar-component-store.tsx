import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  components: [],
});

export default state;
