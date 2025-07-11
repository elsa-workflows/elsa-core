using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Dispatches a domain event.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Asynchronously publishes the specified event using the domain event dispatcher.
    /// </summary>
    Task DispatchAsync(DomainEventNotification domainEvent, CancellationToken cancellationToken = default);
}