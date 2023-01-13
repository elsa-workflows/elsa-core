import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

export interface AuthStore {
  accessToken: string;
  refreshToken: string;
  name: string;
  permissions: Array<string>;
  signedIn: boolean;
}

const {state, onChange} = createStore<AuthStore>({
  accessToken: null,
  refreshToken: null,
  name: null,
  permissions: [],
  signedIn: false
});

export default state;
