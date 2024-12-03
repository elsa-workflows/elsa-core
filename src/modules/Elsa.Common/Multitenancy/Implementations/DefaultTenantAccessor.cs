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
        internal set => CurrentTenantField.Value = value;
    }

    public IDisposable PushContext(Tenant? tenant)
    {
        return new CurrentTenantScope(tenant, Tenant, t => Tenant = t);
    }
}

public class CurrentTenantScope : IDisposable
{
    private readonly Tenant? _previousTenant;
    private readonly Action<Tenant?> _apply;

    public CurrentTenantScope(Tenant? newTenant, Tenant? previousTenant, Action<Tenant?> apply)
    {
        _previousTenant = previousTenant;
        _apply = apply;
        _apply(newTenant);
    }

    public void Dispose()
    {
        _apply(_previousTenant);
    }
}