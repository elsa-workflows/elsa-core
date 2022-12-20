import notificationStore from "./notification-store";
import {NotificationType} from "./models"

export class NotificationService {
    constructor() {
    }

    static createNotification = (notification: NotificationType ): NotificationType => {
        notificationStore.notifications = [...notificationStore.notifications, notification]
        return notification;
    }

   static updateNotification = (notification: NotificationType, obj: NotificationType) => {
       console.log(notification);
       const notifIndex = notificationStore.notifications.findIndex(item => item.id === notification.id)

       const notif = notificationStore.notifications.find(item => item.id === notification.id)
       notif.title = obj.title;
       notif.text = obj.text;

       const filtered = notificationStore.notifications.filter(item => item.id !== notification.id)
       notificationStore.notifications = [...filtered, notificationStore.notifications[notifIndex] = notif]
    }
}
