using Elsa.Tenants.Features;

namespace Elsa.Persistence.EFCore.Modules.Tenants;

/// <summary>
/// Provides extensions to various management related features.
/// </summary>
public static class TenantManagementFeatureExtensions
{
    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static TenantManagementFeature UseEntityFrameworkCore(this TenantManagementFeature feature, Action<EFCoreTenantManagementFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}