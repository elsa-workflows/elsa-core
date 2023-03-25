using Elsa.Identity.Entities;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Validates user credentials.
/// </summary>
public interface IUserCredentialsValidator
{
    /// <summary>
    /// Validates the specified user credentials.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user if the credentials are valid, otherwise <c>null</c>.</returns>
    ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default);
}