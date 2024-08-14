namespace Elsa.Tenants.Services;

/// <inheritdoc />
public class AmbientTenantAccessor : IAmbientTenantAccessor
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
