using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Contracts;

/// <summary>
/// Factory to create a new scope from Service and preserve the current tenant ID from ITenantAccessor.
/// </summary>
public interface ITenantServiceScopeFactory
{
    /// <summary>
    /// Create a new scope from Service and preserve the current tenant ID from ITenantAccessor.
    /// </summary>
    /// <returns>New scope with tenant ID maintained.</returns>
    IServiceScope CreateScopeWithTenant();
}
