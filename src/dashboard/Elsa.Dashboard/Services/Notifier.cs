using System.Collections.Generic;
using Elsa.Dashboard.Models;

namespace Elsa.Dashboard.Services
{
    public class Notifier : INotifier
    {
        private readonly List<Notification> notifications;

        public Notifier()
        {
            notifications = new List<Notification>();
        }

        public IReadOnlyCollection<Notification> Notifications => notifications.AsReadOnly();

        public void Notify(Notification notification)
        {
            notifications.Add(notification);
        }
    }
}