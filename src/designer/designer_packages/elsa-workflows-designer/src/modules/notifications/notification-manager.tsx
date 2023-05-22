import {Component, h, Prop, Watch} from '@stencil/core';
import {leave, toggle} from 'el-transition'
import notificationStore from './notification-store';
import {EventBus} from '../../services';
import NotificationService from './notification-service';

@Component({
  tag: 'elsa-notifications-manager',
  shadow: false,
  styleUrl: '',
})
export class NotificationManager {
  @Prop() public modalState: boolean;

  @Watch('modalState')
  onModalStateChange(value) {
    toggle(this.modal);
  }

  private readonly eventBus: EventBus;

  modal: HTMLElement;
  overlay: HTMLElement;

  static NotificationServiceLocal = NotificationService;

  deleteNotif = (id) => {
    notificationStore.notifications = notificationStore.notifications.filter(item => item.id !== id)
  }

  handleToggle = () => {
    NotificationManager.NotificationServiceLocal.toggleNotification();
    toggle(this.modal);
  };

  private closeMenu() {
    leave(this.modal);
  }

  private toggleMenu() {
    toggle(this.modal);
  }


  render() {
    const {notifications} = notificationStore;

    return (
      <div>
        <div
          ref={el => this.modal = el}
          data-transition-enter="tw-transform tw-transition tw-ease-in-out tw-duration-100 sm:tw-duration-100"
          data-transition-enter-start="tw-translate-x-full"
          data-transition-leave="tw-transform tw-transition tw-ease-in-out tw-duration-100 sm:tw-duration-100"
          data-transition-leave-start="tw-translate-x-0"
          data-transition-leave-end="tw-translate-x-full"
          class='hidden tw-z-50 tw-top-16 tw-fixed tw-inset-y-0 tw-right-0 tw-flex tw-max-w-full tw-pl-10 sm:tw-pl-16'>

          <div class="tw-w-screen tw-max-w-md">
            <div class="tw-flex tw-h-full tw-flex-col tw-overflow-y-scroll tw-bg-white tw-shadow-xl">
              <div class="tw-p-6 tw-border-b">
                <div class="tw-flex tw-items-start tw-justify-between">
                  <h2 class="tw-text-lg tw-font-medium tw-text-gray-900" id="slide-over-title">Notifications</h2>
                  <div class="tw-ml-3 tw-flex tw-h-7 tw-items-center">
                    <button onClick={() => this.handleToggle()}
                            type="button"
                            class="tw-rounded-md tw-bg-white tw-text-gray-400 hover:tw-text-gray-500 focus:tw-ring-2 focus:tw-ring-blue-500">
                      <span class="tw-sr-only">Close panel</span>
                      <svg class="tw-h-6 tw-w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"
                           stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
              {notifications.length === 0 && <div
                class="tw-p-6 tw-pointer-events-auto tw-w-full tw-overflow-hidden tw-border-b tw-ring-1 tw-ring-black tw-ring-opacity-5">There are no notifications</div>}
              <ul role="list"
                  class="tw-pointer-events-auto tw-overflow-y-auto tw-flex-1 tw-divide-y tw-divide-gray-200 tw-overflow-y-auto">
                {notifications.map(notification => (
                  <li>
                    <div class="tw-border-b tw-group tw-relative tw-flex tw-items-center tw-py-6 tw-px-5">
                      <a href="#" class="-tw-m-1 tw-block tw-flex-1 tw-p-1">
                        <elsa-notification-template notification={notification}>
                          <div slot="close-button" class="tw-relative tw-ml-2 tw-inline-block tw-flex-shrink-0 tw-text-left">
                            <button onClick={() => this.deleteNotif(notification.id)}>
                              <svg class="tw-h-6 tw-w-6" xmlns="http://www.w3.org/2000/svg" fill="none"
                                   viewBox="0 0 24 24"
                                   stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                              </svg>
                            </button>
                          </div>
                        </elsa-notification-template>
                      </a>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      </div>
    );
  }
}

