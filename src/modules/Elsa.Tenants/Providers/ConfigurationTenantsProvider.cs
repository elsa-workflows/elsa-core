using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Providers;

/// <summary>
/// Provides the implementation to retrieve tenant information from a configuration.
/// </summary>
public class ConfigurationTenantsProvider : ITenantsProvider
{
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private ICollection<Tenant> _tenants = new List<Tenant>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationTenantsProvider"/> class.
    /// </summary>
    public ConfigurationTenantsProvider(IOptionsMonitor<MultitenancyOptions> options, IConfiguration configuration, ITenantService tenantService)
    {
        _configuration = configuration;
        _tenantService = tenantService;
        UpdateTenants(options.CurrentValue);
        options.OnChange(OnOptionsChanged);
    }

    private async void OnOptionsChanged(MultitenancyOptions options, string? name)
    {
        UpdateTenants(options);
        await _tenantService.RefreshAsync();
    }

    private void UpdateTenants(MultitenancyOptions options)
    {
        var tenants = options.Tenants.ToList();
        
        // Rebind each Tenant's Configuration property manually using array indices
        for (int i = 0; i < tenants.Count; i++) 
            tenants[i].Configuration = _configuration.GetSection($"Multitenancy:Tenants:{i}:Configuration");
        
        _tenants = tenants;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return new(_tenants);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The caller of this method may require dynamic access to the tenant properties.")]
    public ValueTask<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantsQueryable = _tenants.AsQueryable();
        var tenant = filter.Apply(tenantsQueryable).FirstOrDefault();
        return new(tenant);
    }
}
