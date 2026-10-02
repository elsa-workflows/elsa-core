using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides information about the server.
/// </summary>
public interface IServerInformationProvider
{
    /// <summary>
    /// Gets information about the server.
    /// </summary>
    ValueTask<ServerInformation> GetInfoAsync(CancellationToken cancellationToken = default);
}