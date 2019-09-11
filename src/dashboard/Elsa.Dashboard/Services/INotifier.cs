using System.Collections.Generic;
using Elsa.Dashboard.Models;

namespace Elsa.Dashboard.Services
{
    public interface INotifier
    {
        IReadOnlyCollection<Notification> Notifications { get; }
        void Notify(Notification notification);
    }
}