import {h} from '@stencil/core';
import {createStore} from '@stencil/store';
import {DropdownButtonItem} from "../components/shared/dropdown-button/models";

export interface NewButtonItemStore {
  items: Array<DropdownButtonItem>;
  mainItem: DropdownButtonItem;
}

const {state, onChange} = createStore<NewButtonItemStore>({
  items: [],
  mainItem: null
});

export default state;
