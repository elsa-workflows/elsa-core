using Elsa.Mediator.Contracts;
using Elsa.Resilience.Extensions;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Resilience.Handlers;

[UsedImplicitly]
public class PropagateRetryAttemptedFlag : INotificationHandler<BackgroundActivityExecutionCompleted>
{
    public Task HandleAsync(BackgroundActivityExecutionCompleted notification, CancellationToken cancellationToken)
    {
        var hasRetries = notification.ActivityExecutionContext.GetRetriesAttemptedFlag();

        if (hasRetries)
            notification.ActivityExecutionContext.SetRetriesAttemptedFlag(); // Propagates the flag to all ancestors.

        return Task.CompletedTask;
    }
}