import {Component, h, State, Prop, Watch} from "@stencil/core";
import {NotificationType} from "./models";
import notificationStore from "./notification-store";
import NotificationService from "./notification-service";

@Component({
  tag: 'elsa-toast-notification',
  shadow: false,
  styleUrl: '',
})
export class ToastNotification {
  @Prop() public notification: NotificationType;
  @Prop() public showDuration = 6000;
  private timer;

  componentDidRender() {
    this.timer = setTimeout(() => {
      this.hideToast();
    }, this.showDuration);
  }

  handleClick = () => {
    this.hideToast();
  }

  disconnectedCallback() {
    window.clearTimeout(this.timer);
    this.hideToast();
  }

  hideToast = () => {
    NotificationService.hideToast(this.notification);
  }

  render() {
    const {infoPanelBoolean} = notificationStore;

    return (
      (this.notification.showToast !== false && !infoPanelBoolean) ? (
        <div class="mt-2 pr-2 flex w-full flex-col items-center space-y-4 sm:items-end">
          <div
            class="pointer-events-auto w-full max-w-sm rounded-lg z-50 bg-white shadow-lg ring-1 ring-black ring-opacity-5">
            <div class="p-4 z-30">
              <elsa-notification-template notification={this.notification}>
                <siv slot="close-button">
                  <button
                    type="button"
                    onClick={this.handleClick}
                    class="inline-flex rounded-md bg-white text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
                    <span class="sr-only">Close</span>
                    <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"
                         fill="currentColor" aria-hidden="true">
                      <path
                        d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
                    </svg>
                  </button>
                </siv>
              </elsa-notification-template>
            </div>
          </div>
        </div>
      ) : null
    );
  }
}
