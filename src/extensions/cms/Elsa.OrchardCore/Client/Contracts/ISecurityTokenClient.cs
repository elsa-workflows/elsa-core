using Elsa.OrchardCore.Client.Models;

namespace Elsa.OrchardCore.Client.Contracts;

/// <summary>
/// Defines a client responsible for retrieving security tokens from a defined service.
/// </summary>
public interface ISecurityTokenClient
{
    /// <summary>
    /// Asynchronously retrieves a security token using the provided client credentials.
    /// </summary>
    /// <param name="clientId">The client identifier used to request the security token.</param>
    /// <param name="clientSecret">The client secret used to authenticate the request for the security token.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the retrieved <see cref="SecurityToken"/>.</returns>
    Task<SecurityToken> GetSecurityTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
}