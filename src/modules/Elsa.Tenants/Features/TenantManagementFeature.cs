using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Features;

/// <summary>
/// Enables tenant management endpoints.
/// </summary>
public class TenantManagementFeature(IModule serviceConfiguration) : FeatureBase(serviceConfiguration)
{
    private Func<IServiceProvider, ITenantStore> _tenantStoreFactory = sp => sp.GetRequiredService<MemoryTenantStore>();
    
    public TenantManagementFeature WithTenantStore(Func<IServiceProvider, ITenantStore> factory)
    {
        _tenantStoreFactory = factory;
        return this;
    }
    
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<MemoryTenantStore>();
        Services.AddScoped(_tenantStoreFactory);
    }
}