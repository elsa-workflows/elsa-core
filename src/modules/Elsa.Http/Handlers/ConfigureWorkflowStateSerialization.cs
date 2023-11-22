using Elsa.Http.Serialization;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Core.State;
using JetBrains.Annotations;

namespace Elsa.Http.Handlers;

/// <summary>
/// Configures the serialization of <see cref="WorkflowState"/> objects.
/// </summary>
[PublicAPI]
public class ConfigureWorkflowStateSerialization : INotificationHandler<SerializingWorkflowState>, INotificationHandler<SafeSerializerSerializing>
{
    /// <inheritdoc />
    public Task HandleAsync(SerializingWorkflowState notification, CancellationToken cancellationToken)
    {
        notification.SerializerOptions.Converters.Add(new HttpStatusCodeCaseForWorkflowInstanceConverter());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(SafeSerializerSerializing notification, CancellationToken cancellationToken)
    {
        notification.SerializerOptions.Converters.Add(new HttpStatusCodeCaseForWorkflowInstanceConverter());
        return Task.CompletedTask;
    }
}