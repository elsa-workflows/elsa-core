namespace Elsa.Studio.Authentication.Abstractions.Models;

/// <summary>
/// Identifies the authentication provider selected by an Elsa Studio host.
/// </summary>
public enum StudioAuthenticationProvider
{
    ElsaIdentity,
    ElsaLogin,
    OpenIdConnect,
    ExternalAuthentication
}
