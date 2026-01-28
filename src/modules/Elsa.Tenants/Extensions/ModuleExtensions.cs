using Elsa.Features.Services;
using Elsa.Tenants.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Tenants.Extensions;

/// <summary>
/// Extensions for <see cref="IModule"/> that installs the <see cref="TenantsFeature"/> feature.
/// </summary>
[UsedImplicitly]
public static class ModuleExtensions
{
    /// <summary>
    /// Installs and configures the <see cref="TenantsFeature"/> feature.
    /// </summary>
    [UsedImplicitly]
    public static IModule UseTenants(this IModule module, Action<TenantsFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }

    extension(TenantsFeature feature)
    {
        /// <summary>
        /// Installs and configures the <see cref="TenantManagementEndpointsFeature"/> feature.
        /// </summary>
        [UsedImplicitly]
        public TenantsFeature UseTenantManagementEndpoints(Action<TenantManagementEndpointsFeature>? configure = null)
        {
            feature.Module.Configure(configure);
            return feature;
        }

        /// <summary>
        /// Installs and configures the <see cref="TenantManagementFeature"/> feature.
        /// </summary>
        [UsedImplicitly]
        public TenantsFeature UseTenantManagement(Action<TenantManagementFeature>? configure = null)
        {
            feature.Module.Configure(configure);
            return feature;
        }
    }
}