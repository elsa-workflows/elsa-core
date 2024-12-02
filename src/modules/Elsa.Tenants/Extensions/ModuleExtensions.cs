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
    public static IModule UseTenantManagementEndpoints(this IModule module, Action<TenantManagementEndpointsFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }

    /// <summary>
    /// Installs and configures the <see cref="TenantManagementFeature"/> feature.
    /// </summary>
    public static IModule UseTenantManagement(this IModule module, Action<TenantManagementFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}