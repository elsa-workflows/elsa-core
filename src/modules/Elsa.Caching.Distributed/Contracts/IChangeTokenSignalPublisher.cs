namespace Elsa.Caching.Distributed.Contracts;

/// <summary>
/// Represents a service that can publish change token signals.
/// </summary>
public interface IChangeTokenSignalPublisher
{
    /// <summary>
    /// Publishes a change token signal for the specified key.
    /// </summary>
    ValueTask PublishAsync(string key, CancellationToken cancellationToken = default);
}