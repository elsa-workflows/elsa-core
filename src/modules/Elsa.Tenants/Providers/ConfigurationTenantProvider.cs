using Elsa.Tenants.Entities;
using Elsa.Tenants.Models;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Providers;
public class ConfigurationTenantProvider : ITenantProvider
{
    private readonly IOptions<TenantsOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBasedUserProvider"/> class.
    /// </summary>
    public ConfigurationTenantProvider(IOptions<TenantsOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public List<Tenant> GetAllTenants()
    {
        return _options.Value.Tenants.ToList();
    }

    /// <inheritdoc />
    public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantsQueryable = _options.Value.Tenants.AsQueryable();
        var tenant = filter.Apply(tenantsQueryable).FirstOrDefault();
        return Task.FromResult(tenant);
    }
}
