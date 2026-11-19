namespace Elsa.Common.Multitenancy;

/// <summary>Trusted claims used to establish the tenant boundary for a principal.</summary>
public static class TenantPrincipalClaims
{
    /// <summary>The verified tenant ID bound to a credential.</summary>
    public const string BoundTenant = "elsa:bound_tenant";

    /// <summary>Indicates host registry authority when issued by a trusted authentication scheme.</summary>
    public const string HostAdministrator = "elsa:host_administrator";
}
