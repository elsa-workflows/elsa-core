using Elsa.Caching.Contracts;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Services;

/// <summary>
/// Decorates an <see cref="IChangeTokenSignaler"/> and publishes a signal after the signal has been triggered.
/// </summary>
public class DistributedChangeTokenSignaler(IChangeTokenSignaler decoratedSignaler, IChangeTokenSignalPublisher signalPublisher) : IChangeTokenSignaler
{
    /// <inheritdoc />
    public IChangeToken GetToken(string key)
    {
        return decoratedSignaler.GetToken(key);
    }

    /// <inheritdoc />
    public async ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        await decoratedSignaler.TriggerTokenAsync(key, cancellationToken);
        await signalPublisher.PublishAsync(key, cancellationToken);
    }
}