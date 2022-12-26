import notificationStore from "./notification-store";
import {NotificationType} from "./models"

export default class NotificationService {
    constructor() {
    }

    static toogleNotification = () => {
      notificationStore.infoPanelBoolean = !notificationStore.infoPanelBoolean
    }

    static createNotification = (notification: NotificationType ): NotificationType => {
        notificationStore.notifications = [...notificationStore.notifications, notification]
        return notification;
    }

   static updateNotification = (notification: NotificationType, newNotifFields: NotificationType) => {
       const notifIndex = notificationStore.notifications.findIndex(item => item.id === notification.id)

       const notif = notificationStore.notifications.find(item => item.id === notification.id)
       notif.title = newNotifFields.title;
       notif.text = newNotifFields.text;

       const filtered = notificationStore.notifications.filter(item => item.id !== notification.id)
       notificationStore.notifications = [...filtered, notificationStore.notifications[notifIndex] = notif]
    }
}
