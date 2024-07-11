using Elsa.Caching.Distributed.Contracts;
using Elsa.Caching.Distributed.MassTransit.Messages;
using MassTransit;

namespace Elsa.Caching.Distributed.MassTransit.Services;

/// <summary>
/// Represents a service that publishes change token signals using MassTransit.
/// </summary>
public class MassTransitChangeTokenSignalPublisher(IBus bus) : IChangeTokenSignalPublisher
{
    /// <inheritdoc />
    public async ValueTask PublishAsync(string key, CancellationToken cancellationToken = default)
    {
        var message = new TriggerChangeTokenSignal(key);
        await bus.Publish(message, cancellationToken);
    }
}