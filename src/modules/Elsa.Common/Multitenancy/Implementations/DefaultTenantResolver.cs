namespace Elsa.Common.Multitenancy;

/// <summary>
/// An implementation of <see cref="ITenantResolver"/> that always returns the default tenant.
/// </summary>
public class DefaultTenantResolver : ITenantResolver
{
    private readonly Tenant _defaultTenant = new()
    {
        Id = null!,
        Name = "Default"
    };

    public Task<Tenant?> GetTenantAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Tenant?>(_defaultTenant);
    }
}