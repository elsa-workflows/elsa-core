using Elsa.Studio.Login.Contracts;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc />
public class JwtAuthenticationProvider(IJwtAccessor jwtAccessor) : IAuthenticationProvider
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        return await jwtAccessor.ReadTokenAsync(tokenName);
    }
}