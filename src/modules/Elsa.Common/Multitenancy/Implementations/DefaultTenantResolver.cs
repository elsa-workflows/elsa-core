namespace Elsa.Common.Multitenancy;

/// <summary>
/// An implementation of <see cref="ITenantResolver"/> that always returns the default tenant.
/// </summary>
public class DefaultTenantResolver : TenantResolverBase
{
    private readonly Tenant _defaultTenant = new()
    {
        Id = string.Empty,
        Name = "Default"
    };

    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        return Resolved(_defaultTenant.Id);
    }
}