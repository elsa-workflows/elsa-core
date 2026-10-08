namespace Elsa.Extensions;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the service with a specific implementation type only if the combination
    /// of service and implementation does not already exist in the service collection.
    /// </summary>
    public static IServiceCollection TryAddScopedImplementation<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(sd => sd.ServiceType == typeof(TService) && sd.ImplementationType == typeof(TImplementation))) 
            services.AddScoped<TService, TImplementation>();

        return services;
    }

    /// <summary>
    /// Adds the service with a specific implementation factory only if the combination
    /// of service and implementation already doesn't exist.
    /// </summary>
    public static IServiceCollection TryAddScopedImplementation<TService>(
        this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        if (services.All(sd => sd.ServiceType != typeof(TService))) 
            services.AddScoped(implementationFactory);

        return services;
    }
}