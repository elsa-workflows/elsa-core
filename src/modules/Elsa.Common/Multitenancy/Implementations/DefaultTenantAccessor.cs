namespace Elsa.Common.Multitenancy;

/// <summary>
/// Default implementation of <see cref="ITenantAccessor"/>.
/// </summary>
public class DefaultTenantAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<Tenant?> CurrentTenantField = new();

    /// <inheritdoc/>
    public Tenant? Tenant
    {
        get => CurrentTenantField.Value;
        set => CurrentTenantField.Value = value;
    }
}