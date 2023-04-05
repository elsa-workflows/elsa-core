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
          <elsa-toast-notification notification={notification}></elsa-toast-notification>
      );
    } else {
      return null;
    }
  }
}
