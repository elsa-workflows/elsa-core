using Elsa.Common.Contracts;
using Elsa.Tenants.Contracts;

namespace Elsa.Tenants.Accessors;

public class TenantAccessor : ITenantAccessor
{
    private readonly AsyncLocal<string?> _currentTenantId;

    public TenantAccessor()
    {
        _currentTenantId = new AsyncLocal<string?>();
    }

    /// <inheritdoc/>
    public string? GetCurrentTenantId()
    {
        return _currentTenantId.Value;
    }

    /// <inheritdoc/>
    public void SetCurrentTenantId(string? tenantId)
    {
        _currentTenantId.Value = tenantId;
    }
}
