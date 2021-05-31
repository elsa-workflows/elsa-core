using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Events;
using MediatR;

namespace Elsa.Handlers
{
    /// <summary>
    /// Persists runtime activity input & output property values.  
    /// </summary>
    public class PersistActivityPropertyState : INotificationHandler<ActivityExecuted>
    {
        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            var activity = notification.Activity;
            var activityType = activity.GetType();
            var properties = activityType.GetProperties()
                .Where(x => (x.GetCustomAttribute<ActivityInputAttribute>() != null || x.GetCustomAttribute<ActivityOutputAttribute>() != null) && x.GetCustomAttribute<NonPersistableAttribute>() == null);
            var activityExecutionContext = notification.ActivityExecutionContext;

            foreach (var property in properties)
            {
                var value = property.GetValue(activity);

                // TODO: Implement #898 (activity persistence providers).
                activityExecutionContext.SetState(property.Name, value);
            }

            return Task.CompletedTask;
        }
    }
}