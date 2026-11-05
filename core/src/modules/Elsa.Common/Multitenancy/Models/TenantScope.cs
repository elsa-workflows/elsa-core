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

    // Not async: the tenant is restored in an AsyncLocal, and a change made inside an async method does not reach the caller.
    public ValueTask DisposeAsync()
    {
        _tenantContext.Dispose();

        if (ServiceScope is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        ServiceScope.Dispose();
        return ValueTask.CompletedTask;
    }
}