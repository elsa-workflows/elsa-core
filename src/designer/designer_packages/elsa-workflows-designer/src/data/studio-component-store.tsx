import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  activeComponentFactory: () => <elsa-home-page />,
  modalComponents: []
});

export default state;
