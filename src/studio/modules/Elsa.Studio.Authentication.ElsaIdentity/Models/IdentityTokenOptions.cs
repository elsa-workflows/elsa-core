namespace Elsa.Studio.Authentication.ElsaIdentity.Models;

/// <summary>
/// Options used by <see cref="Services.AccessTokenAuthenticationStateProvider"/> when creating the authenticated identity.
/// </summary>
public class IdentityTokenOptions
{
    /// <summary>
    /// The claim type to use for the user's name.
    /// </summary>
    public string NameClaimType { get; set; } = "name";

    /// <summary>
    /// The claim type to use for the user's roles.
    /// </summary>
    public string RoleClaimType { get; set; } = "role";
}

