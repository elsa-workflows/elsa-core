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
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, PermissionAuthorizationHandler>());

        return services;
    }

    /// <summary>Contributes <typeparamref name="T"/>'s permission descriptors to the catalog.</summary>
    public static IServiceCollection AddPermissionDescriptors<T>(this IServiceCollection services) where T : class, IPermissionDescriptorProvider
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionDescriptorProvider, T>());

        return services;
    }
}
