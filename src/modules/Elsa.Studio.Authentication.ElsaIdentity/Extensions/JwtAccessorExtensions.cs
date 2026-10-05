using Elsa.Studio.Authentication.ElsaIdentity.Contracts;

namespace Elsa.Studio.Authentication.ElsaIdentity.Extensions;

/// <summary>
/// Extension methods for <see cref="IJwtAccessor"/>.
/// </summary>
public static class JwtAccessorExtensions
{
    /// <summary>
    /// Removes every token that makes up an ElsaIdentity session from storage.
    /// </summary>
    public static async ValueTask ClearTokensAsync(this IJwtAccessor jwtAccessor)
    {
        await jwtAccessor.ClearTokenAsync(TokenNames.AccessToken);
        await jwtAccessor.ClearTokenAsync(TokenNames.RefreshToken);
        await jwtAccessor.ClearTokenAsync(TokenNames.IdToken);
    }
}
