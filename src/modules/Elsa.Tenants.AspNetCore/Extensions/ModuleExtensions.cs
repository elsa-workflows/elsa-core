using Elsa.Features.Services;
using Elsa.Tenants.AspNetCore.Features;
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
    public static IModule UseTenantsHttpRouting(this IModule module, Action<MultitenantHttpRoutingFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}