using Elsa.Api.Client.Resources.Identity.Models;
using Refit;

namespace Elsa.Api.Client.Resources.Identity.Contracts;

/// <summary>
/// Represents a client for the login API.
/// </summary>
public interface ILoginApi
{
    /// <summary>
    /// Sends the specified request to the login API.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    [Post("/identity/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request, CancellationToken cancellationToken = default);
}