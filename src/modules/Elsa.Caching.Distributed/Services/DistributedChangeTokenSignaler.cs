using Elsa.Caching.Distributed.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Distributed.Services;

/// <summary>
/// Decorates an <see cref="IChangeTokenSignaler"/> and publishes a signal after the signal has been triggered.
/// </summary>
[UsedImplicitly]
public class DistributedChangeTokenSignaler(IChangeTokenSignaler decoratedSignaler, IChangeTokenSignalPublisher signalPublisher) : IChangeTokenSignaler, IChangeTokenSignalInvoker
{
    /// <inheritdoc />
    public IChangeToken GetToken(string key) => decoratedSignaler.GetToken(key);

    /// <inheritdoc />
    public async ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        await decoratedSignaler.TriggerTokenAsync(key, cancellationToken);
        await signalPublisher.PublishAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(string key, CancellationToken cancellationToken) => await decoratedSignaler.TriggerTokenAsync(key, cancellationToken);
}