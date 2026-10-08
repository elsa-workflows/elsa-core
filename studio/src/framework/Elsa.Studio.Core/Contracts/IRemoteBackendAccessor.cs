using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides access to the current remote backend.
/// </summary>
public interface IRemoteBackendAccessor
{
    /// <summary>
    /// Gets or sets the current backend.
    /// </summary>
    RemoteBackend RemoteBackend { get; }
}