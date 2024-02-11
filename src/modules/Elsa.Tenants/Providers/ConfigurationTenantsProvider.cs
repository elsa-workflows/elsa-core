using System.Diagnostics.CodeAnalysis;
using Elsa.Tenants.Entities;
using Elsa.Tenants.Models;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Providers;

/// <summary>
/// Provides the implementation to retrieve tenant information from a configuration.
/// </summary>
public class ConfigurationTenantsProvider : ITenantsProvider
{
    private readonly IOptions<TenantsOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationTenantsProvider"/> class.
    /// </summary>
    public ConfigurationTenantsProvider(IOptions<TenantsOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return new(_options.Value.Tenants.ToList());
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public ValueTask<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantsQueryable = _options.Value.Tenants.AsQueryable();
        var tenant = filter.Apply(tenantsQueryable).FirstOrDefault();
        return new(tenant);
    }
}
