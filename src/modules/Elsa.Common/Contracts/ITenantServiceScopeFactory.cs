using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Contracts;

public interface ITenantServiceScopeFactory
{
    /// <summary>
    /// Create a new scope from Service and preserve currentTenantId from ITenantAccessor
    /// </summary>
    /// <returns>New scope with tenantId maintained</returns>
    IServiceScope CreateScopeWithTenant();
}
