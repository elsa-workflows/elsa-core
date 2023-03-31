import { Component, Host, h } from '@stencil/core';
import notificationStore from "./notification-store";

@Component({
  tag: 'elsa-toast-manager',
  shadow: false,
})

export class ToastManager {

  render() {
    const {notifications} = notificationStore;
    const notification = notifications.find(notification => notification.showToast !== false);

    if (notification) {
      return (
          <elsa-awhile-notifications notification={notification}></elsa-awhile-notifications>
      );
    } else {
      return null;
    }
  }
}
