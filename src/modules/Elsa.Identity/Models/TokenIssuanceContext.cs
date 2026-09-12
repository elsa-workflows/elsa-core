using System.Security.Claims;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Models;

/// <summary>
/// Contains the trusted Elsa identity data to project into a token.
/// </summary>
/// <param name="User">The Elsa user.</param>
/// <param name="Roles">The effective Elsa roles.</param>
/// <param name="Permissions">The effective Elsa permissions.</param>
/// <param name="AdditionalClaims">Additional trusted claims.</param>
/// <param name="ExternalAuthenticationSessionId">The optional external authentication session ID.</param>
public sealed record TokenIssuanceContext(
    User User,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<Claim> AdditionalClaims,
    string? ExternalAuthenticationSessionId = null);
