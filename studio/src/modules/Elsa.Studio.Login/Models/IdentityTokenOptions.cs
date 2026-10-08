using Elsa.Studio.Login.Services;
using System.Security.Claims;

namespace Elsa.Studio.Login.Models;

/// <summary>
/// Provides options to configure how a ClaimsIdentity is created by <see cref="AccessTokenAuthenticationStateProvider"/>
/// </summary>
public class IdentityTokenOptions
{
    /// <summary>
    /// The claim type that is being used to identify the name of the created ClaimsIdentity
    /// </summary>
    public string NameClaimType { get; set; } = ClaimsIdentity.DefaultNameClaimType;

    /// <summary>
    /// The claim type that is being used to identify the role of the created ClaimsIdentity
    /// </summary>
    public string RoleClaimType { get; set; } = ClaimsIdentity.DefaultRoleClaimType;
}