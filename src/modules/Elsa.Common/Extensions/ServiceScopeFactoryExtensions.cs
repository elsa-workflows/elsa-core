using Elsa.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Extensions;
public static class ServiceScopeFactoryExtensions
{
    /// <summary>
    /// Create a new scope from Service and preserve currentTenantId from ITenantAccessor
    /// </summary>
    /// <param name="serviceScopeFactory">ServiceScopeFactory used to create a new ServiceScope</param>
    /// <param name="tenantAccessor">TenantAccesor from the previous scope</param>
    /// <returns>New scope with tenantId maintained</returns>
    public static IServiceScope CreateScopeWithTenant(this IServiceScopeFactory serviceScopeFactory, ITenantAccessor tenantAccessor)
    {
        string? tenantId = tenantAccessor?.GetCurrentTenantId();
        IServiceScope scope = serviceScopeFactory.CreateScope();

        var scopedTenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        scopedTenantAccessor.SetCurrentTenantId(tenantId);

        return scope;
    }
}