using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant scope, which sets the current tenant for the duration of the scope.
/// After the scope is disposed, the original tenant is restored.
/// </summary>
public class TenantScope : IAsyncDisposable
{
    private readonly IServiceScope _serviceScope;

    public TenantScope(IServiceScope serviceScope, ITenantAccessor tenantAccessor, Tenant? tenant)
    {
        _serviceScope = serviceScope;
        tenantAccessor.Tenant = tenant;
    }
    
    public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;

    public async ValueTask DisposeAsync()
    {
        if (_serviceScope is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            _serviceScope.Dispose();
    }
}