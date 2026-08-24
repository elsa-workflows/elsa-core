using System.Reflection;
using Elsa.Authorization;
using Elsa.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions;

/// <summary>Registers Elsa's permission model: the evaluator, the descriptor catalog, and the authorization handler.</summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the single permission evaluator and the descriptor registry. Safe to call more than once, and
    /// safe to call before any module has contributed descriptors.
    /// </summary>
    public static IServiceCollection AddElsaAuthorization(this IServiceCollection services)
    {
        services.TryAddSingleton<IPermissionEvaluator, PermissionEvaluator>();
        services.TryAddSingleton<IPermissionDescriptorRegistry, DefaultPermissionDescriptorRegistry>();
        services.TryAddSingleton<IPermissionGrantValidator, PermissionGrantValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, PermissionAuthorizationHandler>());

        return services;
    }

    /// <summary>
    /// Contributes every <see cref="IPermissionDescriptorProvider"/> declared in <paramref name="assembly"/>.
    /// Discovery is by convention so that a module cannot ship endpoints without the catalog describing them.
    /// </summary>
    public static IServiceCollection AddPermissionDescriptorsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var providerTypes = assembly.GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(IPermissionDescriptorProvider).IsAssignableFrom(x));

        foreach (var providerType in providerTypes)
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IPermissionDescriptorProvider), providerType));

        return services;
    }

    /// <summary>
    /// Contributes every <see cref="IPermissionDescriptorProvider"/> declared in the loaded Elsa assemblies.
    /// </summary>
    /// <remarks>
    /// The module path discovers descriptors from the assemblies it registers with FastEndpoints. The shell
    /// path does not register assemblies that way -- CShells discovers endpoints from features implementing
    /// its own marker interface -- so without this the catalog comes up empty on a shell host, taking role
    /// authoring, introspection and the stored-permission validator with it. Scanning is bounded to loaded
    /// Elsa assemblies and runs once per shell.
    /// </remarks>
    public static IServiceCollection AddPermissionDescriptorsFromLoadedAssemblies(this IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(x => x.GetName().Name?.StartsWith("Elsa.", StringComparison.Ordinal) == true && !x.IsDynamic);

        foreach (var assembly in assemblies)
        {
            try
            {
                services.AddPermissionDescriptorsFromAssembly(assembly);
            }
            catch (ReflectionTypeLoadException)
            {
                // An assembly whose types cannot all be loaded contributes nothing rather than failing startup.
            }
        }

        return services;
    }

    /// <summary>Contributes <typeparamref name="T"/>'s permission descriptors to the catalog.</summary>
    public static IServiceCollection AddPermissionDescriptors<T>(this IServiceCollection services) where T : class, IPermissionDescriptorProvider
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionDescriptorProvider, T>());

        return services;
    }
}
