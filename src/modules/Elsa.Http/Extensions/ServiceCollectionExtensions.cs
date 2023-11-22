using Elsa.Http.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a <see cref="IHttpCorrelationIdSelector"/> implementation to the service collection.
    /// </summary>
    public static IServiceCollection AddHttpCorrelationIdSelector<T>(this IServiceCollection services) where T : class, IHttpCorrelationIdSelector
    {
        services.AddSingleton<IHttpCorrelationIdSelector, T>();
        return services;
    }
    
    /// <summary>
    /// Adds a <see cref="IHttpCorrelationIdSelector"/> implementation to the service collection.
    /// </summary>
    public static IServiceCollection AddHttpCorrelationIdSelector(this IServiceCollection services, Func<IServiceProvider, IHttpCorrelationIdSelector> factory)
    {
        services.AddSingleton(factory);
        return services;
    }
}