using Elsa.Common.Contracts;
using Elsa.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds a memory store for the specified entity.
    /// </summary>
    public static IServiceCollection AddMemoryStore<TEntity, TStore>(this IServiceCollection services) where TStore : class
    {
         services.TryAddSingleton<MemoryStore<TEntity>>();
         services.TryAddScoped<TStore>();
         return services;
    }

    /// <summary>
    /// Adds a serialization options configurator for the specified type.
    /// </summary>
    public static IServiceCollection AddSerializationOptionsConfigurator<T>(this IServiceCollection services) where T : class, ISerializationOptionsConfigurator
    {
        services.AddSingleton<ISerializationOptionsConfigurator, T>();
        return services;
    }
}