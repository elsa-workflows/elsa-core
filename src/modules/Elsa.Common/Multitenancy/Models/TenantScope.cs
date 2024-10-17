using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant scope, which sets the current tenant for the duration of the scope.
/// After the scope is disposed, the original tenant is restored.
/// </summary>
public class TenantScope : IDisposable
{
    private readonly IServiceScope _serviceScope;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly Tenant? _originalTenant;

    public TenantScope(IServiceScope serviceScope, ITenantAccessor tenantAccessor, Tenant? tenant)
    {
        _serviceScope = serviceScope;
        _tenantAccessor = tenantAccessor;
        _originalTenant = tenantAccessor.Tenant;
        _tenantAccessor.Tenant = tenant;
    }
    
    public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;

    public void Dispose()
    {
        _tenantAccessor.Tenant = _originalTenant;
        _serviceScope.Dispose();
    }
}