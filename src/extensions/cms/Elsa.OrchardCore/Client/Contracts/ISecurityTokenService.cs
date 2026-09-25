using Elsa.OrchardCore.Client.Models;

namespace Elsa.OrchardCore.Client.Contracts;

/// <summary>
/// Defines methods for obtaining and refreshing security tokens used for authentication.
/// </summary>
public interface ISecurityTokenService
{
    /// <summary>
    /// Retrieves a security token used for authentication. The token is cached and reused until it expires.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A <see cref="SecurityToken"/> containing access token, token type, and expiration information.</returns>
    Task<SecurityToken> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes and retrieves a new security token for authentication.
    /// Typically used when the current token has expired or is no longer valid.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A <see cref="SecurityToken"/> containing a newly generated access token, token type, and expiration information.</returns>
    Task<SecurityToken> RefreshTokenAsync(CancellationToken cancellationToken = default);
}