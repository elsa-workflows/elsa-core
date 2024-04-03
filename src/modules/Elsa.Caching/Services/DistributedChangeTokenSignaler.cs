using Elsa.Caching.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Services;

/// <summary>
/// Decorates an <see cref="IChangeTokenSignaler"/> and publishes a signal after the signal has been triggered.
/// </summary>
[UsedImplicitly]
public class DistributedChangeTokenSignaler(IChangeTokenSignaler decoratedSignaler, IChangeTokenSignalPublisher signalPublisher) : IChangeTokenSignaler, IDistributedChangeTokenSignaler
{
    /// <inheritdoc />
    public IChangeToken GetToken(string key)
    {
        return decoratedSignaler.GetToken(key);
    }

    /// <inheritdoc />
    public async ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        await TriggerTokenLocalAsync(key, cancellationToken);
        await signalPublisher.PublishAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask TriggerTokenLocalAsync(string key, CancellationToken cancellationToken = default)
    {
        await decoratedSignaler.TriggerTokenAsync(key, cancellationToken);
    }
}