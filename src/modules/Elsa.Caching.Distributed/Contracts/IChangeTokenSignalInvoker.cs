using Elsa.Caching.Distributed.Services;

namespace Elsa.Caching.Distributed.Contracts;

/// <summary>
/// Implemented by the <see cref="DistributedChangeTokenSignaler"/> decorator, allowing distributed handlers to access the local signaler.
/// </summary>
public interface IChangeTokenSignalInvoker
{
    /// <summary>
    /// Triggers a change token signal for the specified key asynchronously.
    /// </summary>
    /// <param name="key">The key of the change token signal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask InvokeAsync(string key, CancellationToken cancellationToken = default);
}