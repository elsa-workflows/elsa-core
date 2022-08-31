import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  accessToken: null,
  name: null,
  permissions: [],
  signedIn: false
});

export default state;
