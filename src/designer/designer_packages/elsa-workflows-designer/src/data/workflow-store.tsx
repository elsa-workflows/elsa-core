import {createStore} from '@stencil/store';

const {state, onChange} = createStore({
  parentWorkflowDefinitionId: '',
  childWorkflowDefinitionId: ''
});

export default state;
