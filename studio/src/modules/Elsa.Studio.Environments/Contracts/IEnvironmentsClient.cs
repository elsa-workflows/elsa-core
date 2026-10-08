using Elsa.Studio.Environments.Models;
using Refit;

namespace Elsa.Studio.Environments.Contracts;

/// <summary>
/// An HTTP client for the Environments API.
/// </summary>
public interface IEnvironmentsClient
{
    /// <summary>
    /// Returns a list of environments from the API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of environments.</returns>
    [Get("/environments")]
    Task<ListEnvironmentsResponse> ListEnvironmentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a response containing list environments.
/// </summary>
public record ListEnvironmentsResponse(ICollection<ServerEnvironment> Environments, string? DefaultEnvironmentName);