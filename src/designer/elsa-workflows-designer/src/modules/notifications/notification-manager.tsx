import {Component, h, Prop, State, Watch} from '@stencil/core';
import { Container } from 'typedi';
import {enter, leave, toggle} from 'el-transition'
import notificationStore from './notification-store';
import { EventBus } from '../../services';
import { Notyf, NotyfNotification } from 'notyf';
import { NotificationType } from './models';
import { NotificationEventTypes } from './event-types';
import NotificationService from './notification-service';
import {Input} from "postcss";

@Component({
  tag: 'elsa-notifications-manager',
  shadow: false,
  styleUrl: 'notification.scss',
})
export class NotificationManager {
  @Prop() public modalState : boolean;

  @Watch('modalState')
  onModalStateChange(value){
    toggle(this.modal);
  }

  private readonly eventBus: EventBus;

  modal: HTMLElement;
  overlay: HTMLElement;

  static NotificationServiceLocal = NotificationService;

  constructor() {
  }

   deleteNotif = (id) => {
     console.log(id);
     notificationStore.notifications = notificationStore.notifications.filter(item => item.id !== id)
  }

  handleToggle = () => {
    NotificationManager.NotificationServiceLocal.toogleNotification();
    toggle(this.modal);
  };

  private closeMenu() {
    leave(this.modal);
  }

  private toggleMenu() {
    toggle(this.modal);
  }


  render() {
    const { notifications } = notificationStore;

    return (
        <div>
              <div
                ref={el => this.modal = el}
                data-transition-enter="transform transition ease-in-out duration-500 sm:duration-700"
                       data-transition-enter-start="translate-x-full"
                       data-transition-leave="transform transition ease-in-out duration-500 sm:duration-700"
                       data-transition-leave-start="translate-x-0"
                       data-transition-leave-end="translate-x-full"
                       class='hidden z-50 pt-65 fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16'>

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
                      {notifications.length === 0 && <div class="p-6 pointer-events-auto w-full overflow-hidden border-b ring-1 ring-black ring-opacity-5">There is no notifications yet</div>}
                      <ul role="list" class=" pointer-events-auto overflow-y-auto flex-1 divide-y divide-gray-200 overflow-y-auto">
                      {notifications.map(notif => (
/*                        <li>
                          <div class="group relative flex items-center py-6 px-">
                                <div class="divide-y divide-gray-200 ml-4 truncate">
                                  <p class="right-0 truncate text-sm text-gray-500">{notif.title}</p>
                                </div>
                            <div class="relative ml-2 inline-block flex-shrink-0 text-left">
                                  <button onClick={() => this.deleteNotif(notif.id)}>
                                    <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none"
                                         viewBox="0 0 24 24"
                                         stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                                      <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                                    </svg>
                                  </button>
                            </div>
                          </div>
                        </li>*/
                        <li>
                          <div class="border-b group relative flex items-center py-6 px-5">
                            <a href="#" class="-m-1 block flex-1 p-1">
                              <div class="relative flex min-w-0 flex-1 items-center">

                      <span class="relative inline-block flex-shrink-0">
                      <svg class="h-6 w-6 text-green-400 z-30" xmlns="http://www.w3.org/2000/svg" fill="none"
                           viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round"
                              d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                      </svg>
                      </span>
                                <div class="ml-4 truncate">
                                  <p class="truncate text-sm font-medium text-gray-900">{notif.title}</p>
                                  <p class="truncate text-sm text-gray-500">{notif.text}</p>
                                </div>
                              </div>
                            </a>
                            <div class="relative ml-2 inline-block flex-shrink-0 text-left">
                              <button onClick={() => this.deleteNotif(notif.id)}>
                                <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none"
                                     viewBox="0 0 24 24"
                                     stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                                  <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                                </svg>
                              </button>

                            </div>
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

