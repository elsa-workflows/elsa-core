namespace Elsa.Caching.Contracts;

/// <summary>
/// Represents a distributed change token signaler that combines the functionality of <see cref="IChangeTokenSignaler"/> and <see cref="IChangeTokenSignalPublisher"/>.
/// </summary>
public interface IDistributedChangeTokenSignaler
{
    /// <summary>
    /// Triggers a local change token for the specified key asynchronously.
    /// </summary>
    ValueTask TriggerTokenLocalAsync(string key, CancellationToken cancellationToken = default);
}