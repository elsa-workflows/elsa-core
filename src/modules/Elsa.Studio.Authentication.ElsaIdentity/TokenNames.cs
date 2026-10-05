namespace Elsa.Studio.Authentication.ElsaIdentity;

/// <summary>
/// Represents the token names.
/// </summary>
public static class TokenNames
{
    /// <summary>
    /// Provides the access token value.
    /// </summary>
    public const string AccessToken = "accessToken";

    /// <summary>
    /// Provides the refresh token value.
    /// </summary>
    public const string RefreshToken = "refreshToken";

    /// <summary>
    /// Provides the ID token value. ElsaIdentity does not issue one, but the storage key is shared with the legacy
    /// ElsaLogin module, so it is cleared alongside the other tokens when the session ends.
    /// </summary>
    public const string IdToken = "idToken";
}