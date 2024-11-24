using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant scope, which sets the current tenant for the duration of the scope.
/// After the scope is disposed, the original tenant is restored.
/// </summary>
public class TenantScope : IAsyncDisposable
{
    public TenantScope(IServiceScope serviceScope, ITenantAccessor tenantAccessor, Tenant? tenant)
    {
        ServiceScope = serviceScope;
        tenantAccessor.Tenant = tenant;
    }
    
    public IServiceScope ServiceScope { get; }
    public IServiceProvider ServiceProvider => ServiceScope.ServiceProvider;

    public async ValueTask DisposeAsync()
    {
        if (ServiceScope is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            ServiceScope.Dispose();
    }
}