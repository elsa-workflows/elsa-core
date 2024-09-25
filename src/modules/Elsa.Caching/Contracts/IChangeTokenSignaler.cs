using Microsoft.Extensions.Primitives;

namespace Elsa.Caching;

/// Provides change tokens for memory caches, allowing code to evict cache entries by triggering a signal.
public interface IChangeTokenSignaler
{
    /// Gets a change token for the specified key.
    IChangeToken GetToken(string key);
    
    /// Triggers the change token for the specified key.
    ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default);
}