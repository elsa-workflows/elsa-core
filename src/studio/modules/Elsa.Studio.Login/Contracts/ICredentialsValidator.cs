using Elsa.Studio.Login.Models;

namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// A validator for credentials.
/// </summary>
public interface ICredentialsValidator
{
    /// <summary>
    /// Validates the credentials.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A <see cref="ValidateCredentialsResult"/> instance.</returns>
    ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}