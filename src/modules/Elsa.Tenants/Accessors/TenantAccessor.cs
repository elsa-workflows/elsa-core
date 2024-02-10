using Elsa.Tenants.Contracts;

namespace Elsa.Tenants.Accessors;

/// <inheritdoc />
public class TenantAccessor : ITenantAccessor
{
    private readonly AsyncLocal<string?> _currentTenantId = new();

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
