using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Providers;

/// <summary>
/// Provides the implementation to retrieve tenant information from a configuration.
/// </summary>
[UsedImplicitly]
public class ConfigurationTenantsProvider : ITenantsProvider
{
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ConfigurationTenantsProvider> _logger;
    private ICollection<Tenant> _tenants = new List<Tenant>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationTenantsProvider"/> class.
    /// </summary>
    public ConfigurationTenantsProvider(IOptionsMonitor<TenantsOptions> options, IConfiguration configuration, ITenantService tenantService, ILogger<ConfigurationTenantsProvider> logger)
    {
        _configuration = configuration;
        _tenantService = tenantService;
        _logger = logger;
        UpdateTenants(options.CurrentValue);
        options.OnChange(OnOptionsChanged);
    }

    private async void OnOptionsChanged(TenantsOptions options, string? name)
    {
        try
        {
            UpdateTenants(options);
            await _tenantService.RefreshAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while updating tenants.");
        }
    }

    private void UpdateTenants(TenantsOptions options)
    {
        var tenants = options.Tenants.ToList();
        
        // Rebind each Tenant's Configuration property manually using array indices
        for (int i = 0; i < tenants.Count; i++) 
            tenants[i].Configuration = _configuration.GetSection($"Multitenancy:Tenants:{i}:Configuration");
        
        _tenants = tenants;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Tenant>>(_tenants);
    }

    /// <inheritdoc />
    public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantsQueryable = _tenants.AsQueryable();
        var tenant = filter.Apply(tenantsQueryable).FirstOrDefault();
        return Task.FromResult(tenant);
    }
}
