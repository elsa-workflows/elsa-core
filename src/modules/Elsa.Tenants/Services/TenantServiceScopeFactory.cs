using Elsa.Tenants.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Services;

/// <inheritdoc/>
public class TenantServiceScopeFactory(ITenantAccessor tenantAccessor, IServiceScopeFactory serviceScopeFactory) : ITenantServiceScopeFactory
{
    /// <inheritdoc/>
    public IServiceScope CreateScopeWithTenant()
    {
        string? tenantId = tenantAccessor?.GetCurrentTenantId();
        IServiceScope scope = serviceScopeFactory.CreateScope();

        var scopedTenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        scopedTenantAccessor.SetCurrentTenantId(tenantId);

        return scope;
    }
}
