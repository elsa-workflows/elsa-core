import {h} from '@stencil/core';
import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  activeComponentFactory: () => <elsa-blank />,
  modalComponents: []
});

export default state;
