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
    this.notyf = new Notyf({ types: [{ type: 'regular', duration: 3000 }] });

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
      this.notyfs.set(e.id, this.notyf.success({ message: e.message, type: 'regular', duration: 400 }));
    }
    notificationStore.notifications = [e, ...notificationStore.notifications];
  };

  handleUpdateNotification = (e: Notification) => {
    if (!this.isOpened) {
      this.notyf.dismiss(this.notyfs.get(e.id));
      this.notyfs.set(e.id, this.notyf.success({ message: e.message, type: 'regular', duration: 4000 }));
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
    console.log(notifications);
    return (

        <div>
          {notifications && <div class='flex w-full flex-col items-center space-y-4 sm:items-end  z-30'>
            {notifications.map(item => (
                <div
                    class="pointer-events-auto w-full max-w-sm rounded-lg  z-30 bg-white shadow-lg ring-1 ring-black ring-opacity-5">
                  <div class="p-4  z-30">
                    <div class="flex items-start  z-30">
                      <div class="flex-shrink-0  z-30">
                        <svg class="h-6 w-6 text-green-400  z-30" xmlns="http://www.w3.org/2000/svg" fill="none"
                             viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round"
                                d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                        </svg>
                      </div>
                      <div class="ml-3 w-0 flex-1 pt-0.5  z-30">
                        <p class="text-sm font-medium text-gray-900">{item.title}</p>
                        <p class="mt-1 text-sm text-gray-500">{item.text}</p>
                      </div>
                      <div class="ml-4 flex flex-shrink-0 z-30">
                        <button
                                type="button"
                                class="inline-flex rounded-md bg-white text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
                          <span class="sr-only">Close</span>
                          <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"
                               fill="currentColor" aria-hidden="true">
                            <path
                                d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
            ))}
          </div>}
              <div     data-transition-enter="transform transition ease-in-out duration-500 sm:duration-700"
                       data-transition-enter-start="translate-x-full"
                       data-transition-leave="transform transition ease-in-out duration-500 sm:duration-700"
                       data-transition-leave-start="translate-x-0"
                       data-transition-leave-end="translate-x-full"
                       class={`${this.isOpened ? 'block':  'hidden'  } pt-65 z-30  fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16`}>

                  <div

                      class={`w-screen max-w-md `}>
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
                      {notifications.length === 0 && <div class="p-6 pointer-events-auto w-full max-w-sm overflow-hidden rounded-lg bg-white shadow-lg ring-1 ring-black ring-opacity-5">There is no notifications yet</div>}
                      <ul role="list" class="overflow-y-auto">
                      {notifications.map(notif => (
                        <li class="divide-y divide-gray-200" >
                          <div class=" space-x-2 group relative flex items-center py-4 px-5">
                                <div class="divide-y divide-gray-200 ml-4 truncate">
                                  <p class="right-0 truncate text-sm text-gray-500">{notif.message}</p>
                                </div>
                            <div class="relative ml-2 inline-block flex-shrink-0 text-left">
                        {/*      <button type="button"*/}
                        {/*              class="group relative inline-flex h-8 w-8 items-center justify-center rounded-full bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"*/}
                        {/*              id="options-menu-0-button" aria-expanded="false" aria-haspopup="true">*/}
                        {/*        <span class="sr-only">Open options menu</span>*/}
                        {/*        <span class="flex h-full w-full items-center justify-center rounded-full">*/}
                        {/*          <svg class="h-5 w-5 text-gray-400 group-hover:text-gray-500"*/}
                        {/*               xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"*/}
                        {/*               aria-hidden="true">*/}
                        {/*    <path*/}
                        {/*        d="M10 3a1.5 1.5 0 110 3 1.5 1.5 0 010-3zM10 8.5a1.5 1.5 0 110 3 1.5 1.5 0 010-3zM11.5 15.5a1.5 1.5 0 10-3 0 1.5 1.5 0 003 0z"/>*/}
                        {/*  </svg>*/}
                        {/*</span>*/}
                        {/*      </button>*/}


                              {/*<div*/}
                              {/*    class="absolute top-0 right-9 z-10 w-48 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none"*/}
                              {/*    role="menu" aria-orientation="vertical" aria-labelledby="options-menu-0-button"*/}
                              {/*    >*/}
                                  <button onClick={() => this.deleteNotif(notif.id)}>
                                    <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none"
                                         viewBox="0 0 24 24"
                                         stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                                      <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                                    </svg></button>

                              {/*</div>*/}
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

