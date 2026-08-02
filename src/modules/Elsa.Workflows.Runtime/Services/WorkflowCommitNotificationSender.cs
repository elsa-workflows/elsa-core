using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Buffers notifications emitted during workflow commit persistence and delegates all other notifications to the mediator.
/// </summary>
public class WorkflowCommitNotificationSender(IMediator mediator, WorkflowCommitNotificationBuffer buffer) : INotificationSender
{
    /// <inheritdoc />
    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        return SendAsync(notification, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(INotification notification, IEventPublishingStrategy? strategy, CancellationToken cancellationToken = default)
    {
        return buffer.TryAdd(notification, strategy) ? Task.CompletedTask : mediator.SendAsync(notification, strategy, cancellationToken);
    }
}
