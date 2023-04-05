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
          data-transition-enter="transform transition ease-in-out duration-100 sm:duration-100"
          data-transition-enter-start="translate-x-full"
          data-transition-leave="transform transition ease-in-out duration-100 sm:duration-100"
          data-transition-leave-start="translate-x-0"
          data-transition-leave-end="translate-x-full"
          class='hidden z-50 top-16 fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16'>

          <div class="w-screen max-w-md">
            <div class="flex h-full flex-col overflow-y-scroll bg-white shadow-xl">
              <div class="p-6 border-b">
                <div class="flex items-start justify-between">
                  <h2 class="text-lg font-medium text-gray-900" id="slide-over-title">Notifications</h2>
                  <div class="ml-3 flex h-7 items-center">
                    <button onClick={() => this.handleToggle()}
                            type="button"
                            class="rounded-md bg-white text-gray-400 hover:text-gray-500 focus:ring-2 focus:ring-indigo-500">
                      <span class="sr-only">Close panel</span>
                      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"
                           stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
              {notifications.length === 0 && <div
                class="p-6 pointer-events-auto w-full overflow-hidden border-b ring-1 ring-black ring-opacity-5">There
                is no notifications yet</div>}
              <ul role="list"
                  class=" pointer-events-auto overflow-y-auto flex-1 divide-y divide-gray-200 overflow-y-auto">
                {notifications.map(notification => (
                  <li>
                    <div class="border-b group relative flex items-center py-6 px-5">
                      <a href="#" class="-m-1 block flex-1 p-1">
                        <elsa-notification-template notification={notification}>
                          <div slot="close-button" class="relative ml-2 inline-block flex-shrink-0 text-left">
                            <button onClick={() => this.deleteNotif(notification.id)}>
                              <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none"
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

