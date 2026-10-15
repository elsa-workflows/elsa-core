using Elsa.Common.Multitenancy;

namespace Elsa.Testing.Shared.Multitenancy;

/// <summary>
/// A tenant accessor for tests that construct tenant-aware services directly.
/// </summary>
/// <remarks>
/// Defaults to the default tenant, which is what an untenanted test expects. The stores it feeds also
/// admit tenant-agnostic and unassigned records, so a test that never sets a tenant on its fixtures keeps
/// seeing them.
/// </remarks>
public sealed class TestTenantAccessor(string tenantId = Tenant.DefaultTenantId) : ITenantAccessor
{
    /// <summary>A shared instance scoped to the default tenant.</summary>
    public static ITenantAccessor Default { get; } = new TestTenantAccessor();

    /// <inheritdoc />
    public string TenantId { get; private set; } = tenantId;

    /// <inheritdoc />
    public Tenant? Tenant { get; private set; }

    /// <inheritdoc />
    public IDisposable PushContext(Tenant? tenant)
    {
        var previousTenant = Tenant;
        var previousTenantId = TenantId;

        Tenant = tenant;
        TenantId = tenant?.Id ?? Tenant.DefaultTenantId;

        return new Restore(() =>
        {
            Tenant = previousTenant;
            TenantId = previousTenantId;
        });
    }

    private sealed class Restore(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
