using Microsoft.Extensions.Primitives;

namespace Elsa.Caching;

/// Triggers the change token associated with the specified key.
public interface IChangeTokenSignalInvoker
{
    /// Gets a change token for the specified key.
    IChangeToken GetToken(string key);
    
    /// Triggers the change token for the specified key.
    ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default);
}