using Elsa.Identity.Constants;

namespace Elsa.Identity.Options;

/// <summary>
/// Represents the identity options.
/// </summary>
public class IdentityOptions
{

    /// <summary>
    /// The issuer to use when creating and validating tokens
    /// </summary>
    public string Issuer { get; set; } = "http://elsa.api";

    /// <summary>
    /// The audience to use when creating and validating tokens
    /// </summary>
    public string Audience { get; set; } = "http://elsa.api";
    
    /// <summary>
    /// Gets or sets the claim type that hold the tenant ID in the user's claims.
    /// If not set, <see cref="ClaimConstants.TenantId" /> will be used
    /// </summary>
    public string? TenantIdClaimsType { get; set; }
}