using Elsa.Studio.Authentication.ElsaIdentity.Models;

namespace Elsa.Studio.Authentication.ElsaIdentity.Contracts;

/// <summary>
/// Validates end-user credentials and returns tokens.
/// </summary>
public interface ICredentialsValidator
{
    /// <summary>
    /// Validates credentials.
    /// </summary>
    ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}

