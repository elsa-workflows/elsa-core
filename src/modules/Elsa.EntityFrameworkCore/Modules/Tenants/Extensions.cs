using Elsa.Tenants.Features;

namespace Elsa.EntityFrameworkCore.Modules.Tenants;

/// <summary>
/// Provides extensions to various management related features.
/// </summary>
public static class TenantManagementFeatureExtensions
{
    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static TenantManagementFeature UseEntityFrameworkCore(this TenantManagementFeature feature, Action<EFCoreTenantManagementFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}