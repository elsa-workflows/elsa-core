using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Elsa.Identity.Constants;

/// <summary>
/// Authentication scheme names used by Elsa identity.
/// </summary>
public static class IdentityAuthenticationSchemes
{
    /// <summary>
    /// The default JWT bearer scheme for API access tokens.
    /// </summary>
    public const string AccessToken = JwtBearerDefaults.AuthenticationScheme;

    /// <summary>
    /// JWT bearer scheme used by the token refresh endpoint.
    /// </summary>
    public const string RefreshToken = "RefreshToken";
}
