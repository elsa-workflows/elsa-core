namespace Elsa.Common.Multitenancy;

public interface ITenantScopeFactory
{
    /// <summary>
    /// Creates a new tenant scope.
    /// </summary>
    /// <param name="tenant">The tenant.</param>
    /// <returns>The tenant scope.</returns>
    TenantScope Create(Tenant? tenant);
}