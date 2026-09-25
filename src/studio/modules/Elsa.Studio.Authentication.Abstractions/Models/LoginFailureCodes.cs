namespace Elsa.Studio.Authentication.Abstractions.Models;

/// <summary>
/// Fixed, non-sensitive outcomes that can be returned to the shared login UI.
/// </summary>
public static class LoginFailureCodes
{
    public const string SignInFailed = "sign_in_failed";
    public const string ExternalSignInFailed = "external_sign_in_failed";
}
