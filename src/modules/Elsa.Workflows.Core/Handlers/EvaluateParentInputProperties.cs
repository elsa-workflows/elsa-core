using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;

namespace Elsa.Workflows.Handlers;

public class EvaluateParentInputProperties : INotificationHandler<InvokingActivityCallback>
{
    public async Task HandleAsync(InvokingActivityCallback notification, CancellationToken cancellationToken)
    {
        // Before invoking the parent activity, make sure its properties are evaluated.
        if (!notification.Parent.GetHasEvaluatedProperties()) 
            await notification.Parent.EvaluateInputPropertiesAsync();
    }
}