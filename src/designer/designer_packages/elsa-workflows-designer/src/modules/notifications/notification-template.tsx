import {Component, Host, h, Prop, State} from '@stencil/core';
import {NotificationDisplayType, NotificationType} from "./models";
import { formatTextWithLineBreaks } from '../../utils';

@Component({
  tag: 'elsa-notification-template',
  shadow: false,
})
export class NotificationTemplate {

  @Prop() public notification: NotificationType;
  @State() time: string;
  private timer: number;


  connectedCallback() {
    this.time = this.notification.timestamp.fromNow();
    this.timer = window.setInterval(() => {
      this.time = this.notification.timestamp.fromNow();
    }, 60 * 1000);
  }

  disconnectedCallback() {
    window.clearInterval(this.timer);
  }

  render() {
    const {notification} = this;
    const {type = NotificationDisplayType.Success} = notification;

    return (
      <div class="tw-flex tw-items-start tw-z-30">
        <div class="tw-flex-shrink-0 tw-z-30">
          {type === NotificationDisplayType.Success ?
            <svg class="tw-h-6 tw-w-6 tw-text-green-400 tw-z-30" xmlns="http://www.w3.org/2000/svg" fill="none"
                 viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>

            </svg> : null
          }
          {type === NotificationDisplayType.InProgress ?
            <svg class="tw-animate-spin tw-h-6 tw-w-6 tw-text-green-400" xmlns="http://www.w3.org/2000/svg"
                 fill="none" viewBox="0 0 24 24">
              <circle class="tw-opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="tw-opacity-75" fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
            </svg> : null
          }
          {type === NotificationDisplayType.Warning ?
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5"
                 stroke="currentColor" class="tw-w-6 tw-h-6 tw-text-orange-600">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z"/>
            </svg>
            : null
          }
          {type === NotificationDisplayType.Error ?
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5"
                 stroke="currentColor" class="tw-w-6 tw-h-6 tw-text-red-400">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z"/>
            </svg>
            : null
          }
        </div>
        <div class="tw-ml-3 tw-w-0 tw-flex-1 tw-pt-0.5 tw-z-30">
          <p class="tw-text-sm tw-font-medium tw-text-gray-900">{notification.title}</p>
          <p class="tw-mt-1 tw-text-sm tw-text-gray-500" innerHTML={formatTextWithLineBreaks(notification.text)}></p>
          <p class="tw-mt-1 tw-text-sm tw-text-gray-700 tw-text-right">{this.time}</p>
        </div>
        <div class="tw-ml-4 tw-flex tw-flex-shrink-0 tw-z-30">
          <slot name="close-button"/>
        </div>
      </div>
    );
  }

}
