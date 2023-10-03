using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core Elsa alterations services.
    /// </summary>
    public static IServiceCollection AddAlterationsCore(this IServiceCollection services)
    {
        services.AddSingleton<IAlterationPlanScheduler, DefaultAlterationPlanScheduler>();
        services.AddSingleton<IAlterationJobRunner, DefaultAlterationJobRunner>();
        services.AddSingleton<IAlterationRunner, DefaultAlterationRunner>();
        return services;
    }

    /// <summary>
    /// Adds an alteration handler.
    /// </summary>
    public static IServiceCollection AddAlterationHandler<T>(this IServiceCollection services) where T: class, IAlterationHandler
    {
        services.AddSingleton<IAlterationHandler, T>();
        return services;
    }
}