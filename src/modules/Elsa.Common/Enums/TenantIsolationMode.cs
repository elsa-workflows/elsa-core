namespace Elsa.Common;

/// <summary>
/// Represents the isolation mode for a tenant.
/// </summary>
public enum TenantIsolationMode
{
    /// <summary>
    /// The tenant shares the application shell. This means that the tenant shares the same ServiceCollection and ServiceProvider as other tenants.
    /// </summary>
    Shared,
    
    /// <summary>
    /// The tenant has its own shell. This also means that the tenant has its own ServiceCollection and ServiceProvider.
    /// </summary>
    Isolated
}