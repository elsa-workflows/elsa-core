using Elsa.Caching.Distributed.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Distributed.Services;

/// Decorates an <see cref="IChangeTokenSignaler"/> and publishes a signal after the signal has been triggered.
[UsedImplicitly]
public class DistributedChangeTokenSignaler(IChangeTokenSignaler decoratedSignaler, IChangeTokenSignalPublisher signalPublisher, IChangeTokenSignalInvoker invoker) : IChangeTokenSignaler
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