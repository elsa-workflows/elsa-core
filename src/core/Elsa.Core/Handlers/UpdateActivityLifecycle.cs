using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Handlers
{
    /// <summary>
    /// Writes a few flags to the activity's state that can be used by tools such as Elsa Dashboard to visualize the activity's state. 
    /// </summary>
    public class UpdateActivityLifecycle : INotificationHandler<ActivityExecuting>, INotificationHandler<ActivityExecutionResultExecuted>
    {
        public Task Handle(ActivityExecuting notification, CancellationToken cancellationToken)
        {
            UpdateLifecycle(notification.ActivityExecutionContext, x => x.Executing = true);
            return Task.CompletedTask;
        }

        public Task Handle(ActivityExecutionResultExecuted notification, CancellationToken cancellationToken)
        {
            UpdateLifecycle(notification.ActivityExecutionContext, x => x.Executed = true);
            return Task.CompletedTask;
        }

        private void UpdateLifecycle(ActivityExecutionContext context, Action<ActivityLifecycle> updateAction)
        {
            const string propertyName = "_Lifecycle";
            var lifecycle = context.GetState(propertyName, () => new ActivityLifecycle());
            updateAction(lifecycle);
            context.SetState(propertyName, lifecycle);
        }
    }
}