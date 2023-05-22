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
        <div class="tw-mt-2 tw-pr-2 tw-flex tw-w-full tw-flex-col tw-items-center tw-space-y-4 sm:tw-items-end">
          <div
            class="tw-pointer-events-auto tw-w-full tw-max-w-sm tw-rounded-lg tw-z-50 tw-bg-white tw-shadow-lg tw-ring-1 tw-ring-black tw-ring-opacity-5">
            <div class="tw-p-4 tw-z-30">
              <elsa-notification-template notification={this.notification}>
                <siv slot="close-button">
                  <button
                    type="button"
                    onClick={this.handleClick}
                    class="tw-inline-flex tw-rounded-md tw-bg-white tw-text-gray-400 hover:tw-text-gray-500 focus:tw-outline-none focus:tw-ring-2 focus:tw-ring-blue-500 focus:tw-ring-offset-2">
                    <span class="tw-sr-only">Close</span>
                    <svg class="tw-h-5 tw-w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"
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
