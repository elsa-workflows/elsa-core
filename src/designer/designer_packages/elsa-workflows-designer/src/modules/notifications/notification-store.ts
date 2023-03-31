import { createStore } from '@stencil/store';
import {NotificationType} from "./models";

const { state, onChange } = createStore({
  notifications: [] as NotificationType[],
  infoPanelBoolean: false,
});

export default state;
