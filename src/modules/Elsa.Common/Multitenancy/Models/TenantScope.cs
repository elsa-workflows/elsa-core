namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant scope, which sets the current tenant for the duration of the scope.
/// After the scope is disposed, the original tenant is restored.
/// </summary>
public class TenantScope : IDisposable
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly Tenant? _originalTenant;

    public TenantScope(ITenantAccessor tenantAccessor, Tenant? tenant)
    {
        _tenantAccessor = tenantAccessor;
        _originalTenant = tenantAccessor.CurrentTenant;
        _tenantAccessor.CurrentTenant = tenant;
    }

    public void Dispose()
    {
        _tenantAccessor.CurrentTenant = _originalTenant;
    }
}