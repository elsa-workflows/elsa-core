using Elsa.Caching.Distributed.Contracts;

namespace Elsa.Caching.Distributed.Services;

/// <summary>
/// A no-op implementation of <see cref="IChangeTokenSignalPublisher"/>.
/// </summary>
public class NoopChangeTokenSignalPublisher : IChangeTokenSignalPublisher
{
    /// <inheritdoc />
    public ValueTask PublishAsync(string key, CancellationToken cancellationToken = default) => default;
}