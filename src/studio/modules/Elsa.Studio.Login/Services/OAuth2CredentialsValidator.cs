using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Validates OAuth2 credentials using a token endpoint, client ID, and optional client secret.
/// </summary>
public class OAuth2CredentialsValidator(OAuth2HttpClient httpClient) : ICredentialsValidator
{
    /// <inheritdoc />
    public async ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var tokenResponse = await httpClient.RequestTokenAsync(username, password, cancellationToken);

        // If the access token is null or empty, the validation failed.
        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            return new(false, null, null);

        // Return the result with the access token and refresh token.
        return new(true, tokenResponse.AccessToken, tokenResponse.RefreshToken);
    }
}
