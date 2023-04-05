import moment from 'moment';
import notificationStore from "./notification-store";
import {NotificationDisplayType, NotificationType} from "./models"

export default class NotificationService {
  constructor() {
  }

  static toggleNotification = () => {
    notificationStore.infoPanelBoolean = !notificationStore.infoPanelBoolean
  }

  static createNotification = (notification: NotificationType): NotificationType => {
    notification.timestamp = moment().clone();
    notificationStore.notifications = [notification, ...notificationStore.notifications]
    return notification;
  }

  static updateNotification = (notification: NotificationType, newNotifFields: NotificationType) => {
    const notifIndex = notificationStore.notifications.findIndex(item => item.id === notification.id)

    const updatedNotification = {...notificationStore.notifications[notifIndex]};
    updatedNotification.title = newNotifFields.title;
    updatedNotification.text = newNotifFields.text;
    updatedNotification.type = newNotifFields.type || NotificationDisplayType.Success;
    updatedNotification.timestamp = moment().clone();

    const filtered = notificationStore.notifications.filter(item => item.id !== notification.id)
    notificationStore.notifications = [updatedNotification, ...filtered]
  }

  static hideToast = (notification: NotificationType) => {
    const index = notificationStore.notifications.findIndex(item => item.id === notification.id);
    if (notificationStore.notifications[index].showToast !== false) {
      const notifications = [...notificationStore.notifications]
      notifications[index] = {...notifications[index], showToast: false};
      notificationStore.notifications = notifications;
    }
  }

  static hideAllNotifications = () => {
    notificationStore.infoPanelBoolean = false;
    notificationStore.notifications = notificationStore.notifications.map((notification) => {
      if (notification.showToast !== false) {
        return {
          ...notification,
          showToast: false
        }
      }

      return notification;
    });
  }
}
