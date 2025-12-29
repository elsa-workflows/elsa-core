using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant scope, which sets the current tenant for the duration of the scope.
/// After the scope is disposed, the original tenant is restored.
/// </summary>
public class TenantScope(IServiceScope serviceScope, ITenantAccessor tenantAccessor, Tenant? tenant) : ITenantScope, IAsyncDisposable
{
    private readonly IDisposable _tenantContext = tenantAccessor.PushContext(tenant);

    public IServiceScope ServiceScope { get; } = serviceScope;
    public IServiceProvider ServiceProvider => ServiceScope.ServiceProvider;

    public async ValueTask DisposeAsync()
    {
        _tenantContext.Dispose();
        if (ServiceScope is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            ServiceScope.Dispose();
    }
}