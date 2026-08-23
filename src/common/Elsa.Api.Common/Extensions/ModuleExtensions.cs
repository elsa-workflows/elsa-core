using System.Reflection;
using Elsa.Features.Services;
using Elsa.Permissions;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/>.
/// </summary>
public static class ModuleExtensions
{
    private static readonly object FastEndpointsAssembliesKey = new();

    /// <summary>
    /// Registers the specified assembly for FastEndpoint assembly discovery.
    /// </summary>
    public static IModule AddFastEndpointsAssembly(this IModule module, Assembly assembly)
    {
        var assemblies = module.Properties.GetOrAdd(FastEndpointsAssembliesKey, () => new HashSet<Assembly>());
        assemblies.Add(assembly);
        return module;
    }

    /// <summary>
    /// Registers the assembly for FastEndpoint assembly discovery using the specified marker type.
    /// </summary>
    public static IModule AddFastEndpointsAssembly<T>(this IModule module) => module.AddFastEndpointsAssembly(typeof(T));

    /// <summary>
    /// Registers the assembly for FastEndpoint assembly discovery using the specified marker type.
    /// </summary>
    public static IModule AddFastEndpointsAssembly(this IModule module, Type markerType) => module.AddFastEndpointsAssembly(markerType.Assembly);

    /// <summary>
    /// Returns all collected assemblies for discovery of endpoints.
    /// </summary>
    public static IEnumerable<Assembly> GetFastEndpointsAssembliesFromModule(this IModule module) => module.Properties.GetOrAdd(FastEndpointsAssembliesKey, () => new HashSet<Assembly>());

    /// <summary>
    /// Adds FastEndpoints to the service container and registers all collected assemblies for discovery of endpoints.
    /// </summary>
    public static IModule AddFastEndpointsFromModule(this IModule module)
    {
        var assemblies = module.GetFastEndpointsAssembliesFromModule().ToList();

        module.Services.AddFastEndpoints(options =>
        {
            options.DisableAutoDiscovery = true;
            options.Assemblies = assemblies;
        });

        // A module's permission descriptors are discovered from the same assemblies as its endpoints, so
        // the catalog necessarily describes exactly the endpoints that exist. Registering them separately
        // per module would let the two drift, which is the failure this model exists to remove.
        module.Services.AddElsaAuthorization();

        foreach (var assembly in assemblies)
            module.Services.AddPermissionDescriptorsFromAssembly(assembly);

        return module;
    }
}