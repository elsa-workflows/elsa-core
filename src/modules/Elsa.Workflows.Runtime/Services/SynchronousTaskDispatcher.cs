using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Relies on the <see cref="INotificationSender"/> to synchronously publish the received request as a domain event.
/// </summary>
public class SynchronousTaskDispatcher : ITaskDispatcher
{
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SynchronousTaskDispatcher(INotificationSender notificationSender) => _notificationSender = notificationSender;

    /// <inheritdoc />
    public async Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken = default) => await _notificationSender.SendAsync(request, cancellationToken);
}