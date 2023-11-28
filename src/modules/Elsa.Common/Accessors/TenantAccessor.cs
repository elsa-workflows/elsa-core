using Elsa.Common.Contracts;

namespace Elsa.Common.Accessors;

public class TenantAccessor : ITenantAccessor
{
    private readonly AsyncLocal<string?> _currentBackgroundWorklowTenantId;

    public TenantAccessor()
    {
        _currentBackgroundWorklowTenantId = new AsyncLocal<string?>();
    }

    /// <inheritdoc/>
    public string? GetCurrentTenantId()
    {
        return _currentBackgroundWorklowTenantId.Value;
    }


    /// <inheritdoc/>
    public void SetCurrentTenantId(string? tenantId)
    {
        _currentBackgroundWorklowTenantId.Value = tenantId;
    }
}
