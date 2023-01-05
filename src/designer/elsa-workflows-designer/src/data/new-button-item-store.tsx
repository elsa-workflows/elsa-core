import {h} from '@stencil/core';
import {createStore} from '@stencil/store';
import {MenuItem, MenuItemGroup} from "../components/shared/context-menu/models";

export interface NewButtonItemStore {
  groups: Array<MenuItemGroup>;
  mainItem: MenuItem;
}

const {state, onChange} = createStore<NewButtonItemStore>({
  groups: [],
  mainItem: null
});

export default state;
