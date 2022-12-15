import { Component, h, State } from '@stencil/core';
import { Container } from 'typedi';
import notificationStore from './notification-store';
import { EventBus } from '../../services';
import { Notyf, NotyfNotification } from 'notyf';
import { Notification } from './models';
import { NotificationEventTypes } from './event-types';

@Component({
  tag: 'elsa-notifications-manager',
  shadow: false,
  styleUrl: 'notification.scss',
})
export class NotificationManager {
  private readonly eventBus: EventBus;
  private readonly notyf: Notyf;

  @State() private isOpened = false;
  private notyfs: Map<string, NotyfNotification> = new Map();

  constructor() {
    this.notyf = new Notyf({ types: [{ type: 'regular', duration: 3000, className: '' }] });

    this.eventBus = Container.get(EventBus);
    this.eventBus.on(NotificationEventTypes.Add, this.handleAddNotification);
    this.eventBus.on(NotificationEventTypes.Update, this.handleUpdateNotification);
    this.eventBus.on(NotificationEventTypes.Toggle, this.handleToggle);
  }

   deleteNotif = (id) => {
     console.log(id);
     notificationStore.notifications = notificationStore.notifications.filter(item => item.id !== id)
  }

  handleAddNotification = (e: Notification) => {
    if (!this.isOpened) {
      this.notyfs.set(e.id, this.notyf.success({ message: e.message, type: 'regular', duration: 4000, className: 'p-3 rounded bg-white shadow-lg' }));
    }
    notificationStore.notifications = [e, ...notificationStore.notifications];
  };

  handleUpdateNotification = (e: Notification) => {
    if (!this.isOpened) {
      this.notyf.dismiss(this.notyfs.get(e.id));
      this.notyfs.set(e.id, this.notyf.success({ message: e.message, type: 'regular', duration: 4000, className: 'p-4 rounded bg-white shadow-lg' }));
    }
    notificationStore.notifications = [e, ...notificationStore.notifications];
  };

  handleToggle = () => {
    if (!this.isOpened) {
      this.notyf.dismissAll();
      this.notyfs.clear();
    }
    this.isOpened = !this.isOpened;
  };

  render() {
    const { notifications } = notificationStore;
    console.log(notifications)
    return (
        <div>
              <div class={`notifications-container ${this.isOpened ? 'block' : 'hidden'}`}>
                <div class="">

                  <div class={`transform transition ease-in-out duration-500 sm:duration-700 pointer-events-auto w-screen max-w-md ${this.isOpened ?  'translate-x-0' : 'translate-x-full' }`}>
                    <div class="flex h-full flex-col overflow-y-scroll bg-white shadow-xl">
                      <div class="p-6">
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
                      {notifications.length === 0 && <div class="m-4 p-3 rounded border border-black bg-white shadow-lg">There is no notifications yet</div>}
                      {notifications.map(notif => (
                      <ul role="list" class="notific_ul flex-1 divide-y divide-gray-200 overflow-y-auto">
                        <li>
                          <div class="group relative flex items-center py-6 px-5">
                            <a href="#" class="-m-1 block flex-1 p-1">
                              <div class="relative flex min-w-0 flex-1 items-center">
                        <span class="relative inline-block flex-shrink-0">
                          <img class="h-10 w-10 rounded-full"
                               src="https://images.unsplash.com/photo-1494790108377-be9c29b29330?ixlib=rb-1.2.1&ixid=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=2&w=256&h=256&q=80"
                               alt=""/>
                            <span
                                class="bg-green-400 absolute top-0 right-0 block h-2.5 w-2.5 rounded-full ring-2 ring-white"
                                aria-hidden="true"></span>
                        </span>
                                <div class="ml-4 truncate">
                                  <p class="truncate text-sm text-gray-500">{notif.message}</p>
                                </div>
                              </div>
                            </a>
                            <div class="relative ml-2 inline-block flex-shrink-0 text-left">
                              <button type="button"
                                      class="group relative inline-flex h-8 w-8 items-center justify-center rounded-full bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
                                      id="options-menu-0-button" aria-expanded="false" aria-haspopup="true">
                                <span class="sr-only">Open options menu</span>
                                <span class="flex h-full w-full items-center justify-center rounded-full">
                                  <svg class="h-5 w-5 text-gray-400 group-hover:text-gray-500"
                                       xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"
                                       aria-hidden="true">
                            <path
                                d="M10 3a1.5 1.5 0 110 3 1.5 1.5 0 010-3zM10 8.5a1.5 1.5 0 110 3 1.5 1.5 0 010-3zM11.5 15.5a1.5 1.5 0 10-3 0 1.5 1.5 0 003 0z"/>
                          </svg>
                        </span>
                              </button>


                              <div
                                  class="absolute top-0 right-9 z-10 w-48 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none"
                                  role="menu" aria-orientation="vertical" aria-labelledby="options-menu-0-button"
                                  >
                                <div class="py-1" role="none">
                                  <button onClick={() => this.deleteNotif(notif.id)}>Delete notification</button>

                                </div>
                              </div>
                            </div>
                          </div>
                        </li>

                      </ul>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
        </div>

    );
  }
}
