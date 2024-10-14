namespace Elsa.Common.Multitenancy;

/// <summary>
/// Default implementation of <see cref="ITenantAccessor"/>.
/// </summary>
public class DefaultTenantAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<Tenant?> CurrentTenantField = new();

    /// <inheritdoc/>
    public Tenant? CurrentTenant
    {
        get => CurrentTenantField.Value;
        set => CurrentTenantField.Value = value;
    }
}