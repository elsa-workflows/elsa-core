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
      <div class={`notifications-container bg-white z-30 right-0 w-80 ${this.isOpened ? 'block' : 'hidden'} `}>
        {notifications.length === 0 && <div class="m-4 p-3 rounded border border-black bg-white shadow-lg">There is no notifications yet</div>}
        {notifications.map(notif => (
          <div class="m-4 p-3 rounded border border-black bg-white shadow-lg">{notif.message}</div>
        ))}
      </div>
    );
  }
}
