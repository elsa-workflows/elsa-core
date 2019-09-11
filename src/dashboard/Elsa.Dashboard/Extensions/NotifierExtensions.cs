using Elsa.Dashboard.Models;
using Elsa.Dashboard.Services;

namespace Elsa.Dashboard.Extensions
{
    public static class NotifierExtensions
    {
        public static void Notify(this INotifier notifier, string message, NotificationType type = NotificationType.Information)
        {
            notifier.Notify(new Notification(message, type));
        }
    }
}