using Elsa.Identity.Entities;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Validates application credentials.
/// </summary>
public interface IApplicationCredentialsValidator
{
    /// <summary>
    /// Validates the specified application credentials.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The application if the credentials are valid, otherwise <c>null</c>.</returns>
    ValueTask<Application?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}