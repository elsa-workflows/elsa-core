import notificationStore from "./notification-store";



interface Notification {
    id?: number | any;
    title: string;
    text: string;
}

export class NotificationService {
    constructor() {
    }

    static createNotification = (notification: Notification ): Notification => {
        notificationStore.notifications = [...notificationStore.notifications, notification]
        return notification;
    }

   static updateNotification = (notification: Notification, obj: Notification) => {
       console.log(notification);
       const notifIndex = notificationStore.notifications.findIndex(item => item.id === notification.id)

       const notif = notificationStore.notifications.find(item => item.id === notification.id)
       notif.title = obj.title;
       notif.text = obj.text;

       const filtered = notificationStore.notifications.filter(item => item.id !== notification.id)
       notificationStore.notifications = [...filtered, notificationStore.notifications[notifIndex] = notif]
    }
}