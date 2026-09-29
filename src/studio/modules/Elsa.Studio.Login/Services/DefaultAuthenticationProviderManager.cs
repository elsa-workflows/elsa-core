using Elsa.Studio.Login.Contracts;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc />
public class DefaultAuthenticationProviderManager(IEnumerable<IAuthenticationProvider> authenticationProviders) : IAuthenticationProviderManager
{
    /// <inheritdoc />
    public async Task<string?> GetAuthenticationTokenAsync(string? tokenName, CancellationToken cancellationToken = default)
    {
        foreach (var authenticationProvider in authenticationProviders)
        {
            var token = await authenticationProvider.GetAccessTokenAsync(tokenName ?? TokenNames.AccessToken, cancellationToken);

            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        return string.Empty;
    }
}