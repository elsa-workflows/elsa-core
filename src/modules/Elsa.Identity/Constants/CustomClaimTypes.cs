namespace Elsa.Identity.Constants;

/// <summary>
/// Constants for claims.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// The tenant ID claim.
    /// </summary>
    public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

    /// <summary>
    /// The external authentication session ID claim.
    /// </summary>
    public const string ExternalAuthenticationSessionId = "elsa:external_authentication_session_id";
}
