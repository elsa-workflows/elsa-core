namespace Elsa.Identity.Constants;

/// <summary>
/// Constants for distinguishing identity token usage.
/// </summary>
public static class TokenUse
{
    /// <summary>
    /// The claim type that stores the intended token usage.
    /// </summary>
    public const string ClaimType = "token_use";

    /// <summary>
    /// Token use value for API bearer access tokens.
    /// </summary>
    public const string Access = "access";

    /// <summary>
    /// Token use value for refresh tokens.
    /// </summary>
    public const string Refresh = "refresh";
}
