using Elsa.Features.Services;
using Elsa.Tenants.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Tenants.Extensions;

/// <summary>
/// Extensions for <see cref="IModule"/> that installs the <see cref="TenantsFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs and configures the <see cref="TenantsFeature"/> feature.
    /// </summary>
    public static IModule UseTenants(this IModule module, Action<TenantsFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }

    /// <summary>
    /// Installs and configures the <see cref="TenantManagementEndpointsFeature"/> feature.
    /// </summary>
    public static TenantsFeature UseTenantManagementEndpoints(this TenantsFeature feature, Action<TenantManagementEndpointsFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Installs and configures the <see cref="TenantManagementFeature"/> feature.
    /// </summary>
    public static TenantsFeature UseTenantManagement(this TenantsFeature feature, Action<TenantManagementFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}